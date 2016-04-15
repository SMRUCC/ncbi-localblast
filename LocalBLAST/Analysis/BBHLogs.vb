﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Linq.Extensions
Imports Microsoft.VisualBasic.Text
Imports Microsoft.VisualBasic.Parallel.Tasks
Imports Microsoft.VisualBasic
Imports Entry = System.Collections.Generic.KeyValuePair(Of
    LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BatchParallel.AlignEntry,
    LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BatchParallel.AlignEntry)
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BatchParallel
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.CsvExports
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BBH
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application

Namespace Analysis

    <PackageNamespace("NCBI.LocalBlast.BBH", Publisher:="amethyst.asuka@gcmodeller.org", Category:=APICategories.ResearchTools, Url:="http://gcmodeller.org")>
    Public Module BBHLogs

        ''' <summary>
        ''' 从文件系统之中加载比对的文件的列表
        ''' </summary>
        ''' <param name="DIR"></param>
        ''' <param name="ext"></param>
        ''' <returns></returns>
        <ExportAPI("LoadEntries")>
        <Extension>
        Public Function LoadEntries(DIR As String, Optional ext As String = "*.txt") As AlignEntry()
            Dim Logs = (From path As String
                        In FileIO.FileSystem.GetFiles(DIR, FileIO.SearchOption.SearchTopLevelOnly, ext).AsParallel
                        Select LogNameParser(path)).ToArray
            Return Logs
        End Function

        <ExportAPI("BBH_Entry.Build")>
        <Extension>
        Public Function BuildBBHEntry(source As List(Of AlignEntry)) As Entry()
            Dim lstPairs As New List(Of Entry)

            Do While source.Count > 0
                Dim First = source.First
                Call source.RemoveAt(Scan0)
                Dim Paired = (From entry In source Where entry.BiDirEquals(First) Select entry).FirstOrDefault
                If Not Paired Is Nothing Then
                    Call source.Remove(Paired)
                    Call lstPairs.Add(New Entry(First, Paired))
                    Call Console.Write(".")
                End If
            Loop

            If lstPairs.Count = 0 Then
                Call $"null bbh paires was found, please check you file name rule is in format like <query>_vs__<subject>!".__DEBUG_ECHO
            End If

            Return lstPairs.ToArray
        End Function

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="DIR">Is a Directory which contains the text file output of the blastp searches.</param>
        ''' <returns></returns>
        <ExportAPI("BBH_Entry.Build")>
        <Extension>
        Public Function BuildBBHEntry(DIR As String) As Entry()
            Dim Source As List(Of AlignEntry) = LoadEntries(DIR).ToList
            Return Source.BuildBBHEntry
        End Function

        ''' <summary>
        ''' 只单独加载单向比对的数据入口点列表
        ''' </summary>
        ''' <param name="DIR"></param>
        ''' <param name="query"></param>
        ''' <returns></returns>
        <ExportAPI("Load.SBHEntry")>
        Public Function LoadSBHEntry(DIR As String, query As String) As String()
            Dim LQuery = FileIO.FileSystem.GetFiles(DIR, FileIO.SearchOption.SearchTopLevelOnly, "*.*").ToArray(AddressOf LogNameParser)
            Dim Paths = (From entry In LQuery.AsParallel
                         Where String.Equals(query, entry.QueryName, StringComparison.OrdinalIgnoreCase)
                         Select entry.FilePath).ToArray
            Return Paths
        End Function

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="Source"></param>
        ''' <param name="EXPORT"></param>
        ''' <param name="QueryGrep"></param>
        ''' <param name="SubjectGrep"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        '''
        <ExportAPI("Export.LogData", Info:="Batch export the log data into the besthit data from the batch blastp operation.")>
        Public Function ExportLogData(<Parameter("Dir.Source")> Source As String,
                                      <Parameter("Dir.Export")> EXPORT As String,
                                      <Parameter("Grep.Query", "Default action is:  Tokens ' ' First")>
                                      Optional QueryGrep As TextGrepScriptEngine = Nothing,
                                      <Parameter("Grep.Subject", "Default action is:  Tokens ' ' First")>
                                      Optional SubjectGrep As TextGrepScriptEngine = Nothing,
                                      <Parameter("Using.UltraLarge.Mode")>
                                      Optional UltraLargeSize As Boolean = False) _
                                      As <FunctionReturns("")> AlignEntry()

            If QueryGrep Is Nothing Then
                QueryGrep = TextGrepScriptEngine.Compile("Tokens ' ' First")
            End If

            If SubjectGrep Is Nothing Then
                SubjectGrep = TextGrepScriptEngine.Compile("Tokens ' ' First")
            End If

            Dim Logs = LoadEntries(Source)

            If UltraLargeSize Then
                Return ExportLogDataUltraLargeSize(Logs, EXPORT, QueryGrep, SubjectGrep)
            Else
                Return ExportLogData(Logs, EXPORT, QueryGrep, SubjectGrep)
            End If
        End Function

        <ExportAPI("Export.LogData.UltraLargeSize", Info:="Batch export the log data into the besthit data from the batch blastp operation.")>
        Public Function ExportLogDataUltraLargeSize(<Parameter("DataList.Logs.Entry")>
                                                    Source As IEnumerable(Of AlignEntry),
                                                    <Parameter("Dir.Export")> EXPORT As String,
                                                    <Parameter("Grep.Query")> Optional QueryGrep As TextGrepScriptEngine = Nothing,
                                                    <Parameter("Grep.Subject")> Optional SubjectGrep As TextGrepScriptEngine = Nothing) As <FunctionReturns("")> AlignEntry()

            If QueryGrep Is Nothing Then QueryGrep = TextGrepScriptEngine.Compile("tokens | first")
            If SubjectGrep Is Nothing Then SubjectGrep = TextGrepScriptEngine.Compile("tokens | first")

            Dim LQuery = (From path As AlignEntry
                          In Source.AsParallel
                          Let InternalOperation = __operation(EXPORT, path, QueryGrep, SubjectGrep)
                          Where Not InternalOperation Is Nothing
                          Select InternalOperation).ToArray
            Call "All of the available besthit data was exported!".__DEBUG_ECHO
            Return LQuery
        End Function

        Private Function __operation(EXPORT As String, Path As AlignEntry, QueryGrep As TextGrepScriptEngine, SubjectGrep As TextGrepScriptEngine) As AlignEntry
            Dim FilePath As String = EXPORT & "/" & IO.Path.GetFileNameWithoutExtension(Path.FilePath) & ".besthit.csv"
            If FileIO.FileSystem.FileExists(FilePath) AndAlso FileIO.FileSystem.GetFileInfo(FilePath).Length > 0 Then
                GoTo RETURN_VALUE
            End If
            Dim OutputLog As BlastPlus.v228 = BlastPlus.Parser.TryParse(Path.FilePath)
            If OutputLog Is Nothing Then
                Return Nothing
            End If
            Call OutputLog.Grep(Query:=QueryGrep.Method, Hits:=SubjectGrep.Method)
            Dim besthitsData As BBH.BestHit() = OutputLog.ExportBestHit
            Call besthitsData.SaveTo(FilePath, False, System.Text.Encoding.ASCII)

RETURN_VALUE:
            Path.FilePath = FilePath
            Return Path
        End Function

        ''' <summary>
        ''' 使用这个函数批量导出sbh数据，假若数据量比较小的话
        ''' </summary>
        ''' <param name="source"></param>
        ''' <param name="EXPORT"></param>
        ''' <param name="queryGrep">假若解析的方法为空，则会尝试使用默认的方法解析标题</param>
        ''' <param name="SubjectGrep"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        '''
        <ExportAPI("Export.LogData.List", Info:="Batch export the log data into the besthit data from the batch blastp operation.")>
        Public Function ExportLogData(<Parameter("DataList.Logs.Entry")>
                                      Source As IEnumerable(Of AlignEntry),
                                      <Parameter("Dir.Export")> EXPORT As String,
                                      <Parameter("Grep.Query")> Optional QueryGrep As TextGrepScriptEngine = Nothing,
                                      <Parameter("Grep.Subject")> Optional SubjectGrep As TextGrepScriptEngine = Nothing) _
            As <FunctionReturns("")> AlignEntry()

            If QueryGrep Is Nothing Then QueryGrep = TextGrepScriptEngine.Compile("tokens | first")
            If SubjectGrep Is Nothing Then SubjectGrep = TextGrepScriptEngine.Compile("tokens | first")

            Dim GrepOperation As GrepOperation = New GrepOperation(QueryGrep.Method, SubjectGrep.Method)
            Dim LQuery = (From path As AlignEntry  ' 从日志文件之中解析出比对结果的对象模型
                          In Source.AsParallel
                          Let OutputLog = BlastPlus.Parser.TryParse(path.FilePath)
                          Where OutputLog IsNot Nothing
                          Select path, OutputLog)
            Call "Load blast output log data internal operation job done!".__DEBUG_ECHO
            Dim LogDataChunk = (From OutputLog In LQuery.AsParallel Select logData = GrepOperation.Grep(OutputLog.OutputLog), OutputLog.path)  ' 进行蛋白质序列对象的标题的剪裁操作
            Call "Internal data trimming operation job done! start to writing data....".__DEBUG_ECHO

            For Each File In LogDataChunk
                Dim besthitsData As BBH.BestHit() = File.logData.ExportBestHit

                'If Not LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BBH.BestHit.IsNullOrEmpty(besthitsData, TrimSelfAligned:=True) Then
                Dim Path As String = EXPORT & "/" & IO.Path.GetFileNameWithoutExtension(File.path.FilePath) & ".besthit.csv"
                File.path.FilePath = Path
                Call besthitsData.SaveTo(Path, False, System.Text.Encoding.ASCII)
                'End If
                Call Console.Write(".")
            Next

            Call "All of the available besthit data was exported!".__DEBUG_ECHO

            Return (From PathEntry In LogDataChunk.AsParallel Select PathEntry.path).ToArray
        End Function

        ''' <summary>
        ''' 批量导出最佳比对匹配结果
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        ''' <param name="Source">单项最佳的两两比对的结果数据文件夹</param>
        ''' <param name="EXPORT">双向最佳的导出文件夹</param>
        ''' <param name="CDSAll">从GBK文件列表之中所导出来的蛋白质信息的汇总表</param>
        '''
        <ExportAPI("Export.Besthits", Info:="Batch export the bbh result")>
        Public Function ExportBidirectionalBesthit(Source As IEnumerable(Of AlignEntry),
                                                   <Parameter("CDS.All.Dump", "Lazy loading task.")>
                                                   CDSAll As Task(Of String, Dictionary(Of String, GeneDumpInfo)),
                                                   <Parameter("DIR.EXPORT")> EXPORT As String,
                                                   <Parameter("Null.Trim")> Optional TrimNull As Boolean = False) As BestHit()
            Return ExportBidirectionalBesthit(Source, EXPORT, CDSAll.GetValue, TrimNull)
        End Function

        ''' <summary>
        ''' 批量导出双向最佳比对匹配结果
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        ''' <param name="Source">单项最佳的两两比对的结果数据文件夹，里面的数据文件都是从blastp里面倒出来的besthit的csv文件</param>
        ''' <param name="EXPORT">双向最佳的导出文件夹</param>
        ''' <param name="CDSInfo">从GBK文件列表之中所导出来的蛋白质信息的汇总表</param>
        '''
        <ExportAPI("Export.Besthits", Info:="Batch export the bbh result")>
        Public Function ExportBidirectionalBesthit(Source As IEnumerable(Of AlignEntry),
                                                   <Parameter("Dir.Export")> EXPORT As String,
                                                   <Parameter("CDS.All.Dump")>
                                                   Optional CDSInfo As Dictionary(Of String, GeneDumpInfo) = Nothing,
                                                   <Parameter("Null.Trim")> Optional TrimNull As Boolean = False) As BestHit()

            Dim Files = (From Path As AlignEntry
                         In Source
                         Let besthitData = Path.FilePath.LoadCsv(Of BBH.BestHit)(False).ToArray
                         Select Path,
                             besthitData).ToDictionary(Function(item) item.Path,
                                                       Function(item) item.besthitData)
            Dim CreateBestHit = (From Path As KeyValuePair(Of AlignEntry, BBH.BestHit())
                                 In Files
                                 Let Data = __export(Source, Path.Key, Files, Path.Value)
                                 Select Path = Path.Key, Data)
            Dim GetDescriptionHandle As BiDirectionalBesthit.GetDescriptionHandle
            If CDSInfo Is Nothing Then
                GetDescriptionHandle = Function(null) ""
            Else
                GetDescriptionHandle = Function(Id As String) If(CDSInfo.ContainsKey(Id), CDSInfo(Id).CommonName, "")
            End If
            Dim GetDescriptionResult = (From item
                                        In CreateBestHit
                                        Let descrMatches = BiDirectionalBesthit.MatchDescription(item.Data, SourceDescription:=GetDescriptionHandle)
                                        Select Path = item.Path,
                                            descrMatches)

            Dim result = GetDescriptionResult.GetAnonymousTypeList

            For Each EntryHit In GetDescriptionResult '保存临时数据
                Dim FileName As String = EXPORT & "/" & IO.Path.GetFileNameWithoutExtension(EntryHit.Path.FilePath) & ".bibesthit.csv"
                EntryHit.Path.FilePath = FileName
                Call EntryHit.descrMatches.SaveTo(FileName, False)
                result += EntryHit
            Next

            Dim Grouped = (From item In result Select item Group By item.Path.QueryName Into Group)        '按照Query分组
            Dim Exports = (From Data In Grouped.AsParallel
                           Let hitData = (From item In Data.Group
                                          Select New KeyValuePair(Of String, Dictionary(Of String, BiDirectionalBesthit))(item.Path.HitName, __getDirectionary(item.descrMatches))).ToArray
                           Select __export(Data.QueryName, hitData)).ToArray   '按照分组将数据导出

            '保存临时数据
            For Each item In Exports
                Dim path As String = EXPORT & "/CompiledBesthits/" & item.QuerySpeciesName & ".xml"
                Call item.GetXml.SaveTo(path)
                path = EXPORT & "/CompiledCsvData/" & item.QuerySpeciesName & ".csv"
                Call item.ExportCsv(TrimNull).Save(path, False)
            Next

            Return Exports
        End Function

        Private Function __getDirectionary(data As BiDirectionalBesthit()) As Dictionary(Of String, BiDirectionalBesthit)
            Return (From x As BiDirectionalBesthit  ' 为什么在这里还是会存在重复的数据？？
                    In data
                    Select x
                    Group x By x.QueryName Into Group) _
                         .ToDictionary(Function(x) x.QueryName,
                                       Function(x) x.Group.First)
        End Function

        ''' <summary>
        ''' 得到最佳双向比对的结果, Top类型
        ''' </summary>
        ''' <param name="Source"></param>
        ''' <param name="Entry"></param>
        ''' <param name="Files"></param>
        ''' <param name="Query"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function __export(Source As IEnumerable(Of AlignEntry),
                                  Entry As AlignEntry,
                                  Files As Dictionary(Of AlignEntry, BBH.BestHit()),
                                  Query As BBH.BestHit()) As BiDirectionalBesthit()

            Dim ReverEntry = Entry.SelectEquals(Source)
            Dim Rever = Files(ReverEntry)
            Dim Result = GetBiDirectBhTop(QueryVsSubject:=Query, SubjectVsQuery:=Rever)

            Return Result
        End Function

        Private Function __export(QuerySpeciesName As String, data As KeyValuePair(Of String, Dictionary(Of String, BiDirectionalBesthit))()) As BestHit
            Dim Result As BestHit = New BestHit With {
                .QuerySpeciesName = QuerySpeciesName
            }
            Dim QueryProteins As String() = data.First.Value.Keys.ToArray   '作为主键的蛋白质编号
            Dim LQuery = (From QueryProtein As String
                          In QueryProteins
                          Let hitCollection = (From HitSpecies As KeyValuePair(Of String, Dictionary(Of String, BiDirectionalBesthit)) In data
                                               Let hitttt = __export(HitSpecies.Value, QueryProtein)
                                               Let hhh As Hit = New Hit With {
                                                        .Tag = HitSpecies.Key,
                                                        .HitName = hitttt.HitName,
                                                        .Identities = hitttt.Identities,
                                                        .Positive = hitttt.Positive
                                                    }
                                               Select desc = hitttt.Description, hhh).ToArray
                          Let hitCol As HitCollection = New HitCollection With {
                              .QueryName = QueryProtein,
                              .Description = hitCollection.First.desc,
                              .Hits = (From ddd In hitCollection Select ddd.hhh).ToArray
                          }
                          Select hitCol).ToArray
            Return New BestHit With {
                .QuerySpeciesName = QuerySpeciesName,
                .Hits = LQuery
            }
        End Function

        Private Function __export(hitSpecies As Dictionary(Of String, BiDirectionalBesthit), queryProt As String) As BiDirectionalBesthit
            If hitSpecies.ContainsKey(queryProt) Then
                Return hitSpecies(queryProt)
            Else
                Call $"QueryProtein {queryProt} not found!".__DEBUG_ECHO
                Return BiDirectionalBesthit.NullValue
            End If
        End Function
    End Module
End Namespace