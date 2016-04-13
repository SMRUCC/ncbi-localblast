Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.CsvExports
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BBH

Namespace NCBIBlastResult

    <[PackageNamespace]("Alignment.Table.Creates.Methods", Publisher:="xie.guigang@gmail.com", Category:=APICategories.UtilityTools)>
    Module AlignmentTableCreates

        ''' <summary>
        ''' BBH的文件夹
        ''' </summary>
        ''' <param name="QueryID"></param>
        ''' <param name="Source"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        ''' 
        <ExportAPI("Alignment.Table.From.bbh.Orthologous")>
        Public Function CreateFromBBHOrthologous(QueryID As String,
                                                 Source As String,
                                                 <Parameter("Query.Info")>
                                                 queryInfo As IEnumerable(Of GeneDumpInfo)) As NCBIBlastResult.AlignmentTable
            Dim Entries = (From path As KeyValuePair(Of String, String) In Source.LoadSourceEntryList({"*.csv"})
                           Let Log = LocalBLAST.Application.BatchParallel.LogNameParser(path.Value)
                           Select ID = path.Key,
                               LogEntry = Log,
                               besthitData = path.Value.LoadCsv(Of BestHit)(False).ToArray).ToArray
            Dim QuerySide = (From entry In Entries Where String.Equals(entry.LogEntry.QueryName, QueryID, StringComparison.OrdinalIgnoreCase) Select entry).ToArray '得到Query的比对方向的数据
            Dim SubjectSide = (From entry In Entries Where String.Equals(entry.LogEntry.HitName, QueryID, StringComparison.OrdinalIgnoreCase) Select entry).ToArray
            Dim BBH = (From query In QuerySide.AsParallel
                       Let subject = query.LogEntry.SelectEquals(SubjectSide, Function(Entry) Entry.LogEntry)
                       Let bbhData = (From bbbbh As BiDirectionalBesthit
                                      In BBHParser.BBHTop(QvS:=query.besthitData, SvQ:=subject.besthitData)
                                      Where bbbbh.Matched
                                      Select bbbbh).ToArray
                       Select query.ID,
                           query.LogEntry,
                           bbhData).ToArray
            Dim queryDict As Dictionary(Of String, GeneDumpInfo) = queryInfo.ToDictionary(Function(item) item.LocusID)
            Dim Hits = (From Genome In BBH Select (From Gene In Genome.bbhData Let QueryGene = queryDict(Gene.QueryName)
                                                   Let row = New HitRecord With {
                                                       .Identity = Gene.Identities,
                                                       .QueryStart = QueryGene.Left,
                                                       .QueryEnd = QueryGene.Right,
                                                       .SubjectIDs = Genome.LogEntry.HitName
                                                   }
                                                   Select row)).MatrixToVector
            Dim Table As New AlignmentTable With {
                .Database = Source,
                .Hits = Hits.ToArray,
                .Program = "BBH",
                .Query = QueryID,
                .RID = Now.ToString
            }
            Return Table
        End Function
    End Module
End Namespace