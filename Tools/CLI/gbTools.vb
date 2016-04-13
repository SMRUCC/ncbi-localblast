Imports System.Runtime.CompilerServices
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.GBFF.Keywords.FEATURES.Nodes
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput
Imports LANS.SystemsBiology.SequenceModel.FASTA
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Language.UnixBash

Partial Module CLI

    <ExportAPI("/Merge.faa", Usage:="/Merge.faa /in <DIR> /out <out.fasta>")>
    Public Function MergeFaa(args As CommandLine.CommandLine) As Integer
        Dim inDIR As String = args - "/in"
        Dim out As String = args.GetValue("/out", inDIR & "/faa.fasta")
        Dim fasta As New FastaFile

        For Each file As String In ls - l - r - ext("*.faa") << FileHandles.OpenHandle(inDIR)
            fasta.AddRange(FastaFile.Read(file))
        Next

        Return fasta.Save(out, Encodings.ASCII)
    End Function

    <ExportAPI("/Export.BlastX", Usage:="/Export.BlastX /in <blastx.txt> [/out <out.csv>]")>
    Public Function ExportBlastX(args As CommandLine.CommandLine) As Integer
        Dim [in] As String = args - "/in"
        Dim out As String = args.GetValue("/out", [in].TrimFileExt & ".blastx.csv")
        Dim blastx As BlastPlus.BlastX.v228_BlastX = BlastPlus.BlastX.TryParseOutput([in])
        Dim result = blastx.BlastXHits
        Return result.SaveTo(out)
    End Function

    <ExportAPI("/Export.gb",
               Info:="Export the *.fna, *.faa, *.ptt file from the gbk file.",
               Usage:="/Export.gb /gb <genbank.gb> [/out <outDIR>]")>
    Public Function ExportPTTDb(args As CommandLine.CommandLine) As Integer
        Dim gb As String = args("/gb")
        Dim out As String = args.GetValue("/out", args("/gb").TrimFileExt)

        For Each x As GBFF.File In GBFF.File.LoadDatabase(gb)
            Call x.__exportTo(out)
        Next

        Return 0
    End Function

    <Extension> Private Sub __exportTo(gb As GBFF.File, out As String)
        Dim PTT = gb.GbffToORF_PTT
        Dim Faa = New SequenceModel.FASTA.FastaFile(gb.ExportProteins)
        Dim Fna = gb.Origin.ToFasta
        Dim GFF = gb.ToGff
        Dim name As String = gb.Source.SpeciesName  ' 
        Dim ffn As FastaFile = gb.GeneNtFasta

        name = name.NormalizePathString(False).Replace(" ", "_") ' blast+程序要求序列文件的路径之中不可以有空格，所以将空格替换掉，方便后面的blast操作
        out = out & "/" & gb.Locus.AccessionID

        Call PTT.Save(out & $"/{name}.ptt")
        Call Fna.SaveTo(out & $"/{name}.fna")
        Call Faa.Save(out & $"/{name}.faa")
        Call GFF.Save(out & $"/{name}.gff")
        Call ffn.Save(out & $"/{name}.ffn")
    End Sub

    <ExportAPI("/add.locus_tag",
               Info:="Add locus_tag qualifier into the feature slot.",
               Usage:="/add.locus_tag /gb <gb.gbk> /prefix <prefix> [/add.gene /out <out.gb>]")>
    <ParameterInfo("/add.gene", True, Description:="Add gene features?")>
    Public Function AddLocusTag(args As CommandLine.CommandLine) As Integer
        Dim gbFile As String = args("/gb")
        Dim prefix As String = args("/prefix")
        Dim out As String = args.GetValue("/out", gbFile.TrimFileExt & $".{prefix}.gb")
        Dim gb = GBFF.File.Load(gbFile)
        Dim LQuery = (From x As Feature In gb.Features
                      Where String.Equals(x.KeyName, "gene") OrElse
                          String.Equals(x.KeyName, "CDS", StringComparison.OrdinalIgnoreCase)
                      Let uid As String = x.Location.UniqueId
                      Select uid,
                          x
                      Group By uid Into Group).ToArray

        Dim idx As Integer = 1

        For Each gene In LQuery
            Dim locusId As String = $"{prefix}_{ConsoleDevice.STDIO.ZeroFill(idx.MoveNext, 4)}"

            For Each feature In gene.Group
                feature.x.SetValue(FeatureQualifiers.locus_tag, locusId)
            Next

            Call Console.Write(".")
        Next

        If args.GetBoolean("/add.gene") Then
            Call gb.Features.AddGenes()
        End If

        Return gb.Save(out, System.Text.Encoding.ASCII).CLICode
    End Function

    <ExportAPI("/add.names", Usage:="/add.names /anno <anno.csv> /gb <genbank.gbk> [/out <out.gbk> /tag <overrides_name>]")>
    Public Function AddNames(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/anno")
        Dim gbFile As String = args("/gb")
        Dim out As String = args.GetValue("/out", inFile.TrimFileExt & "-" & gbFile.BaseName & ".gb")
        Dim tag As String = args.GetValue("/tag", "name")
        Dim annos = inFile.LoadCsv(Of NameAnno)
        Dim gb As GBFF.File = GBFF.File.Load(gbFile)

        For Each anno In annos
            Dim features = gb.Features.GetByLocation(anno.Minimum, anno.Maximum)
            For Each feature As Feature In features
                Call feature.Add(tag, anno.Name)
            Next
        Next

        Return gb.Save(out, Encodings.ASCII.GetEncodings).CLICode
    End Function
End Module

Public Class NameAnno
    Public Property Name As String
    Public Property Minimum As Integer
    Public Property Maximum As Integer
End Class