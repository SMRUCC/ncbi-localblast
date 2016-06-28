﻿#Region "Microsoft.VisualBasic::3b7fe1610cfff58ef91b652b1e933bc3, ..\localblast\LocalBLAST\NCBILocalBlast.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xieguigang (xie.guigang@live.com)
    ' 
    ' Copyright (c) 2016 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System.Runtime.CompilerServices
Imports System.Text.RegularExpressions
Imports LANS.SystemsBiology.Assembly.Expasy.AnnotationsTool
Imports LANS.SystemsBiology.Assembly.Expasy.Database
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BBH
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.RpsBLAST
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput
Imports LANS.SystemsBiology.SequenceModel
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Language.UnixBash
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Parallel.Threads
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Text
Imports Microsoft.VisualBasic.Text.Similarity

''' <summary>
''' ShoalShell API interface for ncbi localblast operations.
''' </summary>
''' <remarks></remarks>
<PackageNamespace("NCBI.LocalBLAST", Category:=APICategories.ResearchTools, Publisher:="xie.guigang@gmail.com")>
Public Module NCBILocalBlast

    ''' <summary>
    ''' 进行快速的字符串模糊匹配来查看比对的结果输出是否是完整的数据，这个函数只是适用于小文本文件
    ''' </summary>
    ''' <param name="query">通过对每一条序列的标题进行比对</param>
    ''' <param name="BlastOUTPUT"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function FastCheckIntegrityProvider(query As FASTA.FastaFile, BlastOUTPUT As String) As Boolean
        If Not FileIO.FileSystem.FileExists(BlastOUTPUT) Then
            Return False
        End If

        Dim Queries As String() =
            LinqAPI.Exec(Of String) <= From strLine As String
                                       In IO.File.ReadAllLines(BlastOUTPUT)
                                       Let Entry As String =
                                           Regex.Match(strLine, "Query\s*=\s*.+").Value
                                       Where Not String.IsNullOrEmpty(Entry)
                                       Select Regex.Replace(Entry, "Query\s*=\s*", "").Trim

        If Queries.Length <> query.NumberOfFasta Then
            Return False
        End If

        Dim CompareLQuery As Boolean =
            LinqAPI.DefaultFirst(Of Boolean)(False) <=
                From fa As FASTA.FastaToken
                In query
                Let InternalIntegrity As Boolean = __integrity(fa, Queries)
                Where Not InternalIntegrity
                Select True
        Return Not CompareLQuery
    End Function

    ''' <summary>
    ''' 返回True表示是完整的
    ''' </summary>
    ''' <param name="Fasta"></param>
    ''' <param name="Queries"></param>
    ''' <returns></returns>
    Private Function __integrity(Fasta As FASTA.FastaToken, Queries As String()) As Boolean
        Dim title As String = Fasta.Title
        Dim GetLQuery As String =
            LinqAPI.DefaultFirst(Of String) <= From Query As String
                                               In Queries
                                               Where FuzzyMatchString.Equals(title, Query)
                                               Select Query
        Return Not String.IsNullOrEmpty(GetLQuery)
    End Function

#If DEBUG Then
    <ExportAPI("test.score_parsing")>
    Public Function ParseScore(s As String) As LocalBLAST.BLASTOutput.ComponentModel.Score
        Return LocalBLAST.BLASTOutput.ComponentModel.Score.TryParse(Of LocalBLAST.BLASTOutput.ComponentModel.Score)(s)
    End Function
#End If

    <ExportAPI("Write.Xml.Blast_Output", Info:="Write the blast output data as a Xml data file.")>
    Public Function SaveBlastOutput(data As IBlastOutput,
                                    <Parameter("Path.SaveTo", "The file path of the blast output xml data will be saved.")>
                                    SaveTo As String) As Boolean
        Return data.Save(SaveTo, Encodings.UTF8)
    End Function

    <ExportAPI("Blast.Version()", Info:="Returns the blast program version.")>
    Public Function Version(Handle As LocalBLAST.InteropService.InteropService) As String
        Dim ver As String = Handle.Version
        Dim str As String = "NCBI.Localblast " & vbCrLf &
                            "   ----> {0}" & vbCrLf &
                            "   ----> version {1}"
        Call Console.WriteLine(str, Handle.BlastBin.ToFileURL, ver)
        ver = Regex.Match(ver, "\d+\.\d+\.\d+").Value
        Return ver
    End Function

    ''' <summary>
    '''
    ''' </summary>
    ''' <param name="handle"></param>
    ''' <param name="NT">核酸序列的fasta文件的文件路径</param>
    ''' <param name="genomeRes">假若目标对象为一个文件，则只进行一个Blastn，假若为一个文件夹，并且其中包含有许多蛋白质序列的fasta文件，则进行批量blastn</param>
    ''' <param name="evalue"></param>
    ''' <param name="output"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    '''
    <ExportAPI("Blastn", Info:="Invoke the batch blastn operations for the target query nt sequence.")>
    Public Function Blastn(<Parameter("LocalBlast.Handle", "The commandline interop services program for the local blast program.")>
                           Handle As LocalBLAST.InteropService.InteropService,
                           <Parameter("Nt.Query", "The query nt sequence fasta file path.")> NT As String,
                           <Parameter("Source.Genomes", "The file path of the subject fasta sequence file or the folder path which contains the fasta data file for invoke the batch mode.")>
                           genomeRes As String,
                           <Parameter("Output.DirOrFile", "The output location for the blastn result, if the subject genome source is a " &
                               "fasta sequence file, then the value of this parameter is the file path of the output file; else if the subject " &
                               "genome source is a directory which contains the fasta sequence data file, then the value of this parameter will " &
                               "be using as a folder path.")> Output As String,
                           <Parameter("E-Value", "The expect value of the alignment result.")> Optional EValue As String = "1e-5",
                           Optional reversed As Boolean = False,
                           Optional numThreads As Integer = -1,
                           Optional TimeInterval As Integer = 1000) As Boolean

        If genomeRes.FileExists Then
            If reversed Then
                Call Handle.FormatDb(NT, Handle.MolTypeNucleotide).Start(WaitForExit:=True)
                Call Handle.Blastn(genomeRes, NT, Output, EValue).Start(WaitForExit:=True)
            Else
                Call Handle.FormatDb(genomeRes, Handle.MolTypeNucleotide).Start(WaitForExit:=True)
                Call Handle.Blastn(NT, genomeRes, Output, EValue).Start(WaitForExit:=True)
            End If
            Return True
        ElseIf FileIO.FileSystem.DirectoryExists(genomeRes) Then
            Dim FastaSource As IEnumerable(Of String) = ls - l - r - wildcards("*.fa", "*.fsa", "*.fasta") <= genomeRes
            Call FileIO.FileSystem.CreateDirectory(Output)
            Return Blastn(Handle,
                          NT,
                          GenomeSource:=FastaSource,
                          EValue:=EValue,
                          outDIR:=Output,
                          reversed:=reversed,
                          numThreads:=numThreads,
                          TimeInterval:=TimeInterval)
        Else
            Throw New Exception($"The value of the blastx protein source ""{genomeRes}"" is not valid!")
        End If
    End Function

    ''' <summary>
    ''' Invoke the batch blastn operations for the target query nt sequence.
    ''' </summary>
    ''' <param name="handle"></param>
    ''' <param name="nt"></param>
    ''' <param name="GenomeSource">The fasta sequence data file path collection.(Fasta序列文件的路径的集合)</param>
    ''' <param name="outDIR"></param>
    ''' <param name="evalue"></param>
    ''' <param name="reversed">假若这个参数为真，则<paramref name="nt"/>参数所指向的fasta序列则会作为参考库</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <ExportAPI("Blastn", Info:="Invoke the batch blastn operations for the target query nt sequence.")>
    Public Function Blastn(<Parameter("LocalBlast.Handle")> Handle As LocalBLAST.InteropService.InteropService,
                           nt As String,
                           <Parameter("Source.Genomes")> GenomeSource As IEnumerable(Of String),
                           <Parameter("Dir.Output")> outDIR As String,
                           <Parameter("E-Value")> Optional EValue As String = "1e-5",
                           <Parameter("Reversed", "If this parameter is TRUE then the nt fasta will be using as the subject reference database.")>
                           Optional reversed As Boolean = False,
                           Optional numThreads As Integer = -1, Optional TimeInterval As Integer = 1000) As Boolean
        If reversed Then
            Call Handle.FormatDb(nt, Handle.MolTypeNucleotide).Start(WaitForExit:=True)
        End If

        Call $"{NameOf(GenomeSource)}:={GenomeSource.Count}".__DEBUG_ECHO

        Dim taskArray As Func(Of Boolean)() =
            LinqAPI.Exec(Of Func(Of Boolean)) <=
                From Subject As String
                In GenomeSource
                Let task As Func(Of Boolean) =
                    Function() __blastn(
                    outDIR,
                    nt,
                    Subject,
                    EValue,
                    reversed,
                    Handle)
                Select task

        Call BatchTask(Of Boolean)(taskArray, numThreads, TimeInterval)

        Return True
    End Function

    Private Function __blastn(outputDIR As String,
                              nt As String,
                              subject As String,
                              evalue As String,
                              reversed As Boolean,
                              handle As LocalBLAST.InteropService.InteropService) As Boolean

        Dim OutLog As String = outputDIR & "/" & IO.Path.GetFileNameWithoutExtension(subject) & ".txt"

        If reversed Then
            Call handle.Blastn(subject, nt, OutLog, evalue).Start(WaitForExit:=True)
        Else
            Call handle.FormatDb(subject, handle.MolTypeNucleotide).Start(WaitForExit:=True)
            Call handle.Blastn(nt, subject, OutLog, evalue).Start(WaitForExit:=True)
        End If

        Return True
    End Function

    ''' <summary>
    '''
    ''' </summary>
    ''' <param name="handle"></param>
    ''' <param name="nt">核酸序列的fasta文件的文件路径</param>
    ''' <param name="proteins">假若目标对象为一个文件，则只进行一个BlastX，假若为一个文件夹，并且其中包含有许多蛋白质序列的fasta文件，则进行批量blastx</param>
    ''' <param name="evalue"></param>
    ''' <param name="output"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    '''
    <ExportAPI("blastx",
               Info:="The proteins parameter can be both of the protein sequence fasta file or a folder which contains the protein fasta source file as the blastx subject. 
               nt parameter is the fasta file path of the nucleotide sequence.")>
    Public Function BlastX(handle As LocalBLAST.InteropService.InteropService,
                           nt As String,
                           proteins As String,
                           output As String,
                           Optional evalue As String = "1e-5") As Boolean
        If proteins.FileExists Then
            Call handle.FormatDb(proteins, handle.MolTypeProtein).Start(WaitForExit:=True)
            Call handle.TryInvoke("blastx", nt, proteins, evalue, output).Start(WaitForExit:=True)
            Return True
        ElseIf FileIO.FileSystem.DirectoryExists(proteins) Then
            Dim FastaSource As IEnumerable(Of String) =
                ls - l - r - wildcards("*.fa", "*.fsa", "*.fasta") <= proteins
            Call FileIO.FileSystem.CreateDirectory(output)
            Return BlastX(handle, nt, proteins:=FastaSource, evalue:=evalue, output:=output)
        Else
            Dim msg As String =
                $"The value of the blastx protein source ""{proteins.ToFileURL}"" Is Not valid!"
            Throw New Exception(msg)
        End If
    End Function

    <Extension> <ExportAPI("blastx")>
    Public Function BlastX(handle As LocalBLAST.InteropService.InteropService,
                           nt As String,
                           proteins As IEnumerable(Of String),
                           output As String,
                           Optional evalue As String = "1e-5") As Boolean

        Dim LQuery As Boolean() =
            LinqAPI.Exec(Of Boolean) <= From subject As String
                                        In proteins.AsParallel
                                        Select __blastX(output, subject, handle, nt, evalue)
        Return Not LQuery.IsNullOrEmpty
    End Function

    Private Function __blastX(output As String,
                              subject As String,
                              handle As LocalBLAST.InteropService.InteropService, nt As String, evalue As String) As Boolean

        Dim OutLog As String = output & "/" & IO.Path.GetFileNameWithoutExtension(subject) & ".txt"

        Call handle.FormatDb(subject, handle.MolTypeProtein).Start(WaitForExit:=True)
        Call handle.TryInvoke("blastx", nt, subject, evalue, OutLog).Start(WaitForExit:=True)
        Return True
    End Function

    ''' <summary>
    ''' Initialize a local blast session handle for your program, you can specific the blast bin location on parameter <paramref name="blastbin"></paramref>
    ''' </summary>
    ''' <param name="blastbin">
    ''' This parameter specific the blast bin location, if this parameter is empty then the function will try to search the blastbin automatically.
    ''' (假若本参数为空，则函数会尝试自动搜索出blast程序的文件夹)
    ''' </param>
    ''' <returns></returns>
    ''' <remarks>目前blast日志分析模块仅仅能够支持2.2.28版本的blast日志的解析</remarks>
    <ExportAPI("localblast.session.handles.New()", Info:="If the <para>blastbin</para> Is Not specific, then the program will search For the blast bin automatically.")>
    Public Function CreateSession(Optional blastbin As String = "") As NCBI.Extensions.LocalBLAST.InteropService.InteropService
        If Not String.IsNullOrEmpty(blastbin) AndAlso
            FileIO.FileSystem.DirectoryExists(blastbin) AndAlso
            FileIO.FileSystem.GetFiles(blastbin, FileIO.SearchOption.SearchTopLevelOnly, "blast*.exe").Count > 1 Then

            Return NCBI.Extensions.LocalBLAST.InteropService.CreateInstance(blastbin, LocalBLAST.InteropService.Program.BlastPlus)
        End If

        Dim Directories As String() = ProgramPathSearchTool.SearchDirectory("blast*", "")
        If Directories.IsNullOrEmpty Then
            Return Nothing
        End If

        Dim BLAST As String = Directories.First

        If FileIO.FileSystem.GetFiles(BLAST, FileIO.SearchOption.SearchTopLevelOnly, "blast*.exe").Count > 1 Then
            Return NCBI.Extensions.LocalBLAST.InteropService.CreateInstance(BLAST, LocalBLAST.InteropService.Program.BlastPlus)
        End If

        Dim EXEList As String() = ProgramPathSearchTool.SearchProgram(BLAST, "blast")

        If EXEList.Length > 1 Then
            BLAST = ProgramPathSearchTool.GetMostAppreancePath(EXEList)
            Return NCBI.Extensions.LocalBLAST.InteropService.CreateInstance(BLAST, LocalBLAST.InteropService.Program.BlastPlus)
        Else
            Return Nothing
        End If
    End Function

    <ExportAPI("blastp")>
    Public Function Blastp(session As LocalBLAST.InteropService.InteropService, Query As String, Db As String, Evalue As String, BlastOutput As String) As String
        If String.IsNullOrEmpty(BlastOutput) Then
            BlastOutput = My.Computer.FileSystem.SpecialDirectories.Temp & "/blast_output.log"
        Else
            Call FileIO.FileSystem.CreateDirectory(FileIO.FileSystem.GetParentPath(BlastOutput))
        End If

        Call session.FormatDb(Db, session.MolTypeProtein).Start(WaitForExit:=True)
        Call session.Blastp(Query, Db, BlastOutput, Evalue).Start(WaitForExit:=True)
        Return BlastOutput
    End Function

    <ExportAPI("Read.BlastX.Output")>
    Public Function LoadBlastXOutput(Path As String) As BlastPlus.BlastX.v228_BlastX
        Return BlastPlus.BlastX.OutputReader.TryParseOutput(Path)
    End Function

    <ExportAPI("Read.Blast.Output")>
    Public Function LoadBlastOutput(path As String) As BlastPlus.v228
        Return NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus.Parser.TryParse(path)
    End Function

    ''' <summary>
    '''
    ''' </summary>
    ''' <param name="path"></param>
    ''' <param name="chunk_size">是以1024*1024为基础的，本参数的值应该小于768，最大不应该超过800，否则程序会崩溃.对于1GB以内的日志文件，可以考虑100</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <ExportAPI("Read.Ultra_large_blast_output",
               Info:="chunk_size parameter Is recommended Using 100 When the file size Is below 2GB And Using 768 When the file size Is large than 20GB")>
    Public Function LoadUltraLargeSizeBlastOutput(path As String, Optional chunk_size As Integer = 768) As BlastPlus.v228
        Return NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus.Parser.TryParseUltraLarge(path, CHUNK_SIZE:=chunk_size * 1024 * 1024)
    End Function

    <ExportAPI("Script.Compile",
               Info:="The script line should be this format:  script_tokens1;script_tokens2;....  if there is any space in the script line, then the space should wrapped by the ' character.")>
    Public Function CreateGrepScript(script As String) As TextGrepScriptEngine
        Return TextGrepScriptEngine.Compile(script)
    End Function

    <ExportAPI("Grep.Query")>
    Public Function GrepQuery(blast_output As BlastPlus.v228, script As TextGrepScriptEngine) As BlastPlus.v228
        Call blast_output.Grep(script.Method, Nothing)
        Return blast_output
    End Function

    <ExportAPI("Grep.Hits")>
    Public Function Grephits(blast_output As BlastPlus.v228, script As TextGrepScriptEngine) As BlastPlus.v228
        Call blast_output.Grep(Nothing, script.Method)
        Return blast_output
    End Function

    <ExportAPI("Export.Besthit", Info:="Exports all of the besthit from the blastp output")>
    Public Function ExportBesthit(blast_output As BlastPlus.v228, Optional saveto As String = "", Optional identities As Double = 0.15) As DocumentStream.File
        Dim bh As DocumentStream.File = blast_output.ExportAllBestHist(identities).ToCsvDoc
        If Not String.IsNullOrEmpty(saveto) Then Call bh.Save(saveto, False)
        Return bh
    End Function

    <ExportAPI("Export.Overview.Csv")>
    Public Function ExportOverviewCsv(blastOutput As IBlastOutput, saveto As String) As Boolean
        Dim Oview = blastOutput.ExportOverview
        Return Oview.GetExcelData.SaveTo(saveto, False)
    End Function

    <ExportAPI("Read.Csv.Blast.Overviews")>
    Public Function LoadOverview(path As String) As Views.Overview
        Return Views.Overview.LoadExcel(path)
    End Function

    <ExportAPI("Export.Besthit")>
    Public Function ExportBesthits(data As Views.Overview, Optional identities As Double = 0.15) As BestHit()
        Return data.ExportAllBestHist(identities)
    End Function

    <ExportAPI("Export.bbh")>
    Public Function Export_BidirBesthit(Qvs As IEnumerable(Of BestHit), Svq As IEnumerable(Of BestHit), Optional saveCsv As String = "") As DocumentStream.File
        Dim bibh = BBHParser.GetDirreBhAll2(Svq.ToArray, Qvs.ToArray)
        If Not String.IsNullOrEmpty(saveCsv) Then Call bibh.SaveTo(saveCsv, False)
        Return bibh.ToCsvDoc(False)
    End Function

    <ExportAPI("Export.bbh.Csv")>
    Public Function Export_BidirBesthit(qvs As DocumentStream.File, svq As DocumentStream.File, <Parameter("Save.Csv")> Optional saveCsv As String = "") As DocumentStream.File
        Dim bibh = BBHParser.GetDirreBhAll(svq, qvs)
        If Not String.IsNullOrEmpty(saveCsv) Then Call bibh.Save(saveCsv, False)
        Return bibh
    End Function

    <ExportAPI("Write.Csv.Besthit")>
    Public Function WriteBesthit(data As IEnumerable(Of BestHit), saveto As String) As Boolean
        Return data.SaveTo(saveto, False)
    End Function

    <ExportAPI("Read.Csv.Besthits")>
    Public Function LoadBesthitCsv(path As String) As BestHit()
        Return path.LoadCsv(Of BestHit)(False).ToArray
    End Function

    <ExportAPI("Read.Csv.bbh")>
    Public Function LoadBiDirBh(path As String) As BiDirectionalBesthit()
        Return path.LoadCsv(Of BiDirectionalBesthit)(False).ToArray
    End Function

    <ExportAPI("Write.Csv.bbh")>
    Public Function SaveBBH(data As IEnumerable(Of BiDirectionalBesthit), saveCsv As String) As Boolean
        Return data.SaveTo(saveCsv, False)
    End Function

    <ExportAPI("Enzyme.Classify")>
    Public Function ClassifyEnzyme(Expasy As NomenclatureDB, bh As BestHit()) As T_EnzymeClass_BLAST_OUT()
        Return BBHParser.EnzymeClassification(Expasy, bh)
    End Function

    <ExportAPI("Read.Csv.Myva")>
    Public Function ReadMyvaCOG(path As String) As MyvaCOG()
        Return path.LoadCsv(Of MyvaCOG)(False).ToArray
    End Function

    <ExportAPI("Create.Myva_COG", Info:="blast_output parameter is the original blast output file path.")>
    Public Function MyvaCogClassify(blast_output As String, query_grep As String, Whog_Xml As String) As MyvaCOG()
        Dim textEngine = TextGrepScriptEngine.Compile(query_grep).Method
        Return ClassifyCOGs.Get_MyvaCOG_Classify(blast_output, textEngine, Whog_Xml)
    End Function
End Module
