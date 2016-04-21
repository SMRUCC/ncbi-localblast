Imports LANS.SystemsBiology.Assembly.KEGG.DBGET.bGetObject
Imports LANS.SystemsBiology.NCBI.Extensions.Analysis
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv

Partial Module CLI

    <ExportAPI("/SSBH2BH_LDM",
               Usage:="/SSBH2BH_LDM /in <ssbh.csv> [/xml /out <out.xml>]")>
    Public Function KEGGSSOrtholog2Bh(args As CommandLine) As Integer
        Dim [in] As String = args("/in")
        Dim out As String = args.GetValue("/out", [in].TrimFileExt & ".BestHit.Xml")
        Dim isXml As Boolean = args.GetBoolean("/xml")
        Dim Xml As HitCollection

        If isXml Then
            Dim ssbh As SSDB.OrthologREST = [in].LoadXml(Of SSDB.OrthologREST)
            Xml = KEGG_API.Export(ssbh)
        Else
            Dim ssbh As IEnumerable(Of SSDB.Ortholog) = [in].LoadCsv(Of SSDB.Ortholog)
            Xml = KEGG_API.Export(ssbh, [in].BaseName)
        End If

        Return Xml.SaveAsXml(out).CLICode
    End Function
End Module