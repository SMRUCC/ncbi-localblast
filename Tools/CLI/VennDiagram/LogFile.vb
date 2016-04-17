﻿Imports LANS.SystemsBiology.NCBI.Extensions.Analysis
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ConsoleDevice.STDIO
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.Extensions
Imports Microsoft.VisualBasic.Text
Imports Microsoft.VisualBasic
Imports LANS.SystemsBiology.Localblast.Extensions.VennDiagram.BlastAPI

Partial Module CLI

    ''' <summary>
    ''' 分析BLAST程序所输出的日志文件，目标日志文件必须是经过Grep操作得到的
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>最后一个文件对中的File2位最后一个基因组的标号集合</remarks>
    <ExportAPI("logs_analysis", Info:="Parsing the xml format blast log into a csv data file that use for venn diagram drawing.",
        Usage:="logs_analysis -d <xml_logs_directory> -export <export_csv_file>",
        Example:="logs_analysis -d ~/xml_logs -export ~/Desktop/result.csv")>
    <ParameterInfo("-d",
        Description:="The data directory which contains the xml format blast log file, those xml format log file were generated from the 'venn -> blast' command.",
        Example:="~/xml_logs")>
    <ParameterInfo("-export",
        Description:="The save file path for the venn diagram drawing data csv file.",
        Example:="~/Documents/8004_venn.csv")>
    Public Function Analysis(args As CommandLine.CommandLine) As Integer
        Dim CsvFile As String = args("-export")
        Dim LogsDir As String = args("-d")

        Dim ListFile = LogsPair.GetXmlFileName(LogsDir).LoadXml(Of LogsPair)()
        Dim ListCsv = New List(Of DocumentStream.File())  '每一个文件对中的File1位主要的文件
        For Each List In ListFile.Logs
            Dim Query = From Pair In List Select LogAnalysis.TakeBestHits(NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput.Load(Pair.File1), NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput.Load(Pair.File2)) '获取BestHit
            Call ListCsv.Add(Query.ToArray)
        Next
        Dim LastFile = NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput.Load(ListFile.Logs.Last.Last.File2)
        Call ListCsv.Add(New DocumentStream.File() {(From Query In LastFile.Queries.AsParallel Select Query.QueryName).ToArray})

        Dim MergeResult = (From List In ListCsv Select LogAnalysis.Merge(dataset:=List)).ToList
        Dim Csv = CLI.__mergeFile(MergeResult)  '合并文件，获取最终绘制文氏图所需要的数据文件

        Return Csv.Save(Path:=CsvFile).CLICode
    End Function

    ''' <summary>
    ''' 解析BLAST日志文件中的标记号名称
    ''' </summary>
    ''' <param name="args"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <ExportAPI("grep", Info:="The gene id in the blast output log file are not well format for reading and program processing, so " &
                                 "before you generate the venn diagram you should call this command to parse the gene id from the log file. " &
                                 "You can also done this id parsing job using other tools.",
        Usage:="grep -i <xml_log_file> -q <script_statements> -h <script_statements>",
        Example:="grep -i C:\Users\WORKGROUP\Desktop\blast_xml_logs\1__8004_ecoli_prot.log.xml -q ""tokens | 4"" -h ""'tokens | 2';'tokens ' ' 0'""")>
    <ParameterInfo("-q", False,
        Description:="The parsing script for parsing the gene_id from the blast log file, this switch value is consist of sevral operation " &
                     "tokens, and each token is separate by the ';' character and the token unit in each script token should seperate by " &
                     "the ' character.\n" &
                     "There are two basic operation in this parsing script:\n" &
                     " tokens - Split the query or hit name string into sevral piece of string by the specific delimiter character and " &
                     "          get the specifc location unit in the return string array.\n" &
                     "   Usage:   tokens <delimiter> <position>\n" &
                     "   Example: tokens | 3" &
                     " match - match a gene id using a specific pattern regular expression.\n" &
                     "   usage:   match <regular_expression>\n" &
                     "   Example: match .+[-]\d{5}",
        Example:="'tokens | 5';'match .+[-].+'")>
    <ParameterInfo("-h",
        Description:="The parsing script for parsing the gene_id from the blast log file, this switch value is consist of sevral operation " &
                     "tokens, and each token is separate by the ';' character and the token unit in each script token should seperate by " &
                     "the ' character.\n" &
                     "There are two basic operation in this parsing script:\n" &
                     " tokens - Split the query or hit name string into sevral piece of string by the specific delimiter character and " &
                     "          get the specifc location unit in the return string array.\n" &
                     "   Usage:   tokens <delimiter> <position>\n" &
                     "   Example: tokens | 3" &
                     " match - match a gene id using a specific pattern regular expression.\n" &
                     "   usage:   match <regular_expression>\n" &
                     "   Example: match .+[-]\d{5}",
        Example:="'tokens | 5';'match .+[-].+'")>
    Public Function Grep(args As CommandLine.CommandLine) As Integer
        Dim GrepScriptQuery As TextGrepScriptEngine = TextGrepScriptEngine.Compile(args("-q"))
        Dim GrepScriptHit As TextGrepScriptEngine = TextGrepScriptEngine.Compile(args("-h"))
        Dim XmlFile As String = args("-i")

        If String.IsNullOrEmpty(XmlFile) Then
            Return -1
        End If

        Using File As NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput =
            NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput.Load(XmlFile) 'Depose 操作的时候会自动保存
            Call File.Grep(Query:=AddressOf GrepScriptQuery.Grep, Hits:=AddressOf GrepScriptHit.Grep)
        End Using

        Return 0
    End Function
End Module