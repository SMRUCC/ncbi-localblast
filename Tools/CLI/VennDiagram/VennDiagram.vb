Imports LANS.SystemsBiology.Assembly.KEGG.DBGET.bGetObject
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv

Partial Module CLI

    <ExportAPI("/SSBH2BH_LDM",
               Usage:="/SSBH2BH_LDM /in <ssbh.csv> [/out <out.xml>]")>
    Public Function KEGGSSOrtholog2Bh(args As CommandLine) As Integer
        Dim [in] As String = args("/in")
        Dim out As String = args.GetValue("/out", [in].TrimFileExt & ".BestHit.Xml")
        Dim ssbh As IEnumerable(Of SSDB.Ortholog) = [in].LoadCsv(Of SSDB.Ortholog)
    End Function
End Module