Imports System.Text.RegularExpressions
Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.Linq
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus.BlastX
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.CsvExports

Namespace NCBIBlastResult

    Public Class AlignmentTable : Inherits ITextFile
        Implements ISaveHandle

        <XmlAttribute> Public Property Program As String
        Public Property Query As String
        <XmlAttribute> Public Property RID As String
        Public Property Database As String
        Public Property Hits As HitRecord()

        Public Overrides Function ToString() As String
            Return $"[{RID}]  {Program} -query {Query} -database {Database}  // {Hits.Count} hits found."
        End Function

        Public Function GetHitsEntryList() As String()
            Const LOCUS_ID As String = "(emb|gb|dbj)\|[a-z]+\d+"

            Dim LQuery As String() = (From item As HitRecord
                                      In Me.Hits
                                      Let hitID As String = Regex.Match(item.SubjectIDs, LOCUS_ID, RegexOptions.IgnoreCase).Value
                                      Where Not String.IsNullOrEmpty(hitID)
                                      Select hitID.Split(CChar("|")).Last Distinct).ToArray
            Return LQuery
        End Function

        ''' <summary>
        ''' 按照GI编号进行替换
        ''' </summary>
        ''' <param name="Info"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function DescriptionSubstituted(Info As gbEntryBrief()) As Integer
            Dim GiDict = Info.ToDictionary(Function(item) item.GI)
            Dim LQuery = (From hitEntry As HitRecord In Hits Select __substituted(hitEntry, GiDict)).ToArray
            Hits = LQuery
            Return Hits.Length
        End Function

        Private Shared Function __substituted(hitEntry As HitRecord, dictGI As Dictionary(Of String, gbEntryBrief)) As HitRecord
            Dim GetEntry = (From id As String In hitEntry.GI Where dictGI.ContainsKey(id) Select dictGI(id)).ToArray
            If Not GetEntry.IsNullOrEmpty Then
                hitEntry.SubjectIDs = String.Format("gi|{0}|{1}", GetEntry.First.GI, GetEntry.First.Definition)
            End If
            Return hitEntry
        End Function

        Public Sub TrimLength(MaxLength As Integer)
            Dim avgLength As Integer = (From hit As HitRecord In Hits Select Len(hit.SubjectIDs)).ToArray.Average * 1.3

            If avgLength > MaxLength AndAlso MaxLength > 0 Then
                avgLength = MaxLength
            End If

            For Each hit In Hits
                If Len(hit.SubjectIDs) > avgLength Then
                    hit.SubjectIDs = Mid(hit.SubjectIDs, 1, avgLength)
                End If
            Next
        End Sub

        ''' <summary>
        ''' 按照基因组编号进行替换
        ''' </summary>
        ''' <param name="Info"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function DescriptionSubstituted2(Info As gbEntryBrief()) As Integer
            Dim GiDict As Dictionary(Of String, gbEntryBrief) = Info.ToDictionary(Function(item) item.Locus)
            Dim LQuery = (From hitEntry As HitRecord In Hits Select __substituted2(hitEntry, GiDict)).ToArray
            Hits = LQuery
            Return Hits.Length
        End Function

        Private Shared Function __substituted2(hitEntry As HitRecord, GiDict As Dictionary(Of String, gbEntryBrief)) As HitRecord
            If GiDict.ContainsKey(hitEntry.SubjectIDs) Then
                Dim GetEntry = GiDict(hitEntry.SubjectIDs)
                hitEntry.SubjectIDs = String.Format("gi|{0}|{1}", GetEntry.GI, GetEntry.Definition)
            End If
            Return hitEntry
        End Function

        Public Shared Function LoadDocument(path As String) As AlignmentTable
            Dim docBuffer As String() = (From s As String In System.IO.File.ReadAllLines(path)
                                         Where Not String.IsNullOrEmpty(s)
                                         Select s).ToArray
            Dim head As String() = (From s As String In docBuffer Where InStr(s, "# ") = 1 Select s).ToArray
            docBuffer = docBuffer.Skip(head.Length).ToArray
            Dim Hits = (From s As String In docBuffer.AsParallel Select HitRecord.Mapper(s)).ToArray
            Dim HeadDict = (From s As String In head
                            Let t = Strings.Split(s, ": ")
                            Select Key = t.First,
                                Value = t.Last).ToDictionary(Function(x) x.Key)

            Return New AlignmentTable With {
                .Hits = Hits,
                .FilePath = path,
                .Program = head.First.Trim.Split.Last,
                .Query = HeadDict("# Query").Value,
                .Database = HeadDict("# Database").Value,
                .RID = HeadDict("# RID").Value
            }
        End Function

        Public Overrides Function Save(Optional Path As String = "", Optional encoding As System.Text.Encoding = Nothing) As Boolean Implements ISaveHandle.Save
            Return Me.GetXml.SaveTo(Path, encoding)
        End Function

        ''' <summary>
        ''' 导出绘制的顺序
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>这里不能够使用并行拓展</remarks>
        Public Function ExportOrderByGI() As String()
            Dim LQuery = (From hit As HitRecord In Hits Select hit.GI.FirstOrDefault Distinct).ToArray
            Return LQuery
        End Function

        Private Shared Function __createFromBlastn(sId As String, out As v228) As HitRecord()
            Dim LQuery = (From Query As Query
                          In out.Queries
                          Select __createFromBlastn(sId, Query.SubjectHits)).ToArray
            Dim result = LQuery.MatrixToVector
            Return result
        End Function

        Private Shared Function __createFromBlastn(sId As String, hits As SubjectHit()) As HitRecord()
            Dim LQuery = (From hspNT As SubjectHit
                          In hits
                          Let row As HitRecord = New HitRecord With {
                              .Identity = hspNT.Score.Identities.Value,
                              .DebugTag = hspNT.Name,
                              .SubjectIDs = sId,
                              .BitScore = hspNT.Score.RawScore,
                              .QueryStart = hspNT.QueryLocation.Left,
                              .QueryEnd = hspNT.QueryLocation.Right
                          }
                          Select row).ToArray
            Return LQuery
        End Function

        Public Shared Function CreateFromBlastn(sourceDIR As String) As AlignmentTable
            Dim Files = (From path As String
                         In FileIO.FileSystem.GetFiles(sourceDIR, FileIO.SearchOption.SearchAllSubDirectories, "*.txt")
                         Let XOutput = Parser.LoadBlastOutput(path)
                         Where Not XOutput Is Nothing AndAlso
                             Not XOutput.Queries.IsNullOrEmpty
                         Select ID = path.BaseName,
                             XOutput).ToArray
            Dim LQuery As HitRecord() = (From file In Files Select __createFromBlastn(file.ID, file.XOutput)).MatrixToVector
            Dim Tab As AlignmentTable = New AlignmentTable With {
                .Hits = LQuery,
                .Query = (From file In Files
                          Let Q As Query() =
                              file.XOutput.Queries
                          Where Not Q.IsNullOrEmpty
                          Select Q.First.QueryName).FirstOrDefault,
                .RID = Now.ToShortDateString,
                .Program = "BLASTN",
                .Database = sourceDIR
            }
            Return Tab
        End Function

        Public Shared Function CreateFromBlastX(source As String) As AlignmentTable
            Dim Files = (From path As String
                         In FileIO.FileSystem.GetFiles(source, FileIO.SearchOption.SearchAllSubDirectories, "*.txt")
                         Select ID = path.BaseName,
                             XOutput = OutputReader.TryParseOutput(path)).ToArray
            Dim LQuery = (From file In Files Select (From Query As BlastX.Components.Query
                                                     In file.XOutput.Queries
                                                     Select (From hsp As BlastX.Components.HitFragment
                                                             In Query.Hits
                                                             Let row As HitRecord = New HitRecord With {
                                                                 .Identity = hsp.Score.Identities.Value,
                                                                 .DebugTag = Query.SubjectName,
                                                                 .SubjectIDs = file.ID,
                                                                 .BitScore = hsp.Score.RawScore,
                                                                 .QueryStart = hsp.Hsp.First.Query.Left,
                                                                 .QueryEnd = hsp.Hsp.Last.Query.Right
                                                             }
                                                             Select row).ToArray
                                                         )
                                                 ).MatrixAsIterator.MatrixToVector
            Dim Tab = New AlignmentTable With {
                .Hits = LQuery,
                .Query = Files.First.XOutput.Queries.First.QueryName,
                .RID = Now.ToShortDateString,
                .Program = "BlastX",
                .Database = source
            }
            Return Tab
        End Function
    End Class
End Namespace