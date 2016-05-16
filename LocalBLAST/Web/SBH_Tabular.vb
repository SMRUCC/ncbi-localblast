Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.CsvExports
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BBH
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BatchParallel

Namespace NCBIBlastResult

    <[PackageNamespace]("SBH_Tabular",
                        Publisher:="xie.guigang@gmail.com",
                        Category:=APICategories.UtilityTools)>
    Public Module SBH_Tabular

        ''' <summary>
        ''' BBH的文件夹
        ''' </summary>
        ''' <param name="QueryID"></param>
        ''' <param name="sbhDIR">Directory path which it contains the sbh result data.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        ''' 
        <ExportAPI("Alignment.Table.From.bbh.Orthologous")>
        Public Function CreateFromBBHOrthologous(QueryID As String,
                                                 sbhDIR As String,
                                                 <Parameter("Query.Info")> queryInfo As IEnumerable(Of GeneDumpInfo)) As AlignmentTable

            Dim Entries = (From path As KeyValuePair(Of String, String)
                           In sbhDIR.LoadSourceEntryList({"*.csv"})
                           Let Log As AlignEntry = LogNameParser(path.Value)
                           Select ID = path.Key,
                               LogEntry = Log,
                               besthitData = path.Value.LoadCsv(Of BestHit)(False).ToArray).ToArray
            Dim querySide = From entry In Entries
                            Where String.Equals(entry.LogEntry.QueryName, QueryID, StringComparison.OrdinalIgnoreCase)
                            Select entry '得到Query的比对方向的数据
            Dim hitSide = (From entry In Entries
                           Where String.Equals(entry.LogEntry.HitName, QueryID, StringComparison.OrdinalIgnoreCase)
                           Select entry).ToArray
            Dim BBH = (From query
                       In querySide.AsParallel
                       Let subject = query.LogEntry.SelectEquals(hitSide, Function(Entry) Entry.LogEntry)
                       Let bbhData As BiDirectionalBesthit() = (From bbbbh As BiDirectionalBesthit
                                                                In BBHParser.GetBBHTop(qvs:=query.besthitData, svq:=subject.besthitData)
                                                                Where bbbbh.Matched
                                                                Select bbbbh).ToArray
                       Select query.ID,
                           query.LogEntry,
                           bbhData).ToArray
            Dim queryDict As Dictionary(Of String, GeneDumpInfo) = queryInfo.ToDictionary(Function(item) item.LocusID)
            Dim hits As HitRecord() = (From Genome In BBH
                                       Select (From Gene As BiDirectionalBesthit
                                               In Genome.bbhData
                                               Let QueryGene As GeneDumpInfo = queryDict(Gene.QueryName)
                                               Let row = New HitRecord With {
                                                   .Identity = Gene.Identities,
                                                   .QueryStart = QueryGene.Left,
                                                   .QueryEnd = QueryGene.Right,
                                                   .SubjectIDs = Genome.LogEntry.HitName
                                               }
                                               Select row)).MatrixToVector
            Dim Table As New AlignmentTable With {
                .Database = sbhDIR,
                .Hits = hits.ToArray,
                .Program = "BBH",
                .Query = QueryID,
                .RID = Now.ToString
            }
            Return Table
        End Function
    End Module
End Namespace