Imports System.Runtime.CompilerServices
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus.BlastX
Imports Microsoft.VisualBasic.Linq

Namespace NCBIBlastResult

    Public Module ParserAPI

        Public Function LoadDocument(path As String) As AlignmentTable
            Dim docBuffer As String() = (From s As String In path.ReadAllLines
                                         Where Not String.IsNullOrEmpty(s)
                                         Select s).ToArray
            Dim head As String() = (From s As String In docBuffer Where InStr(s, "# ") = 1 Select s).ToArray
            Dim hits As HitRecord() = (From s As String
                                       In docBuffer.Skip(head.Length).AsParallel
                                       Select HitRecord.Mapper(s)).ToArray
            Dim headAttrs As Dictionary(Of String, String) = (From s As String In head
                                                              Let t = Strings.Split(s, ": ")
                                                              Select Key = t.First,
                                                                  Value = t.Last) _
                                                                 .ToDictionary(Function(x) x.Key,
                                                                               Function(x) x.Value)
            Return New AlignmentTable With {
                .Hits = hits,
                .FilePath = path,
                .Program = head.First.Trim.Split.Last,
                .Query = headAttrs("# Query"),
                .Database = headAttrs("# Database"),
                .RID = headAttrs("# RID")
            }
        End Function

        Private Function __createFromBlastn(sId As String, out As v228) As HitRecord()
            Dim LQuery = (From Query As Query
                          In out.Queries
                          Select __createFromBlastn(sId, Query.SubjectHits)).ToArray
            Dim result = LQuery.MatrixToVector
            Return result
        End Function

        Private Function __createFromBlastn(sId As String, hits As SubjectHit()) As HitRecord()
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

        Public Function CreateFromBlastn(sourceDIR As String) As AlignmentTable
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

        Public Function CreateFromBlastX(source As String) As AlignmentTable
            Dim Files = (From path As String
                         In FileIO.FileSystem.GetFiles(source, FileIO.SearchOption.SearchAllSubDirectories, "*.txt")
                         Select ID = path.BaseName,
                             XOutput = OutputReader.TryParseOutput(path)).ToArray
            Dim LQuery = (From file In Files Select file.ID.__hits(file.XOutput)).MatrixToVector
            Dim Tab = New AlignmentTable With {
                .Hits = LQuery,
                .Query = Files.First.XOutput.Queries.First.QueryName,
                .RID = Now.ToShortDateString,
                .Program = "BlastX",
                .Database = source
            }
            Return Tab
        End Function

        <Extension> Private Iterator Function __hits(id As String, out As v228_BlastX) As IEnumerable(Of HitRecord)
            Yield (From Query As BlastX.Components.Query
                   In out.Queries
                   Select (From hsp As BlastX.Components.HitFragment
                           In Query.Hits
                           Let row As HitRecord = New HitRecord With {
                               .Identity = hsp.Score.Identities.Value,
                               .DebugTag = Query.SubjectName,
                               .SubjectIDs = id,
                               .BitScore = hsp.Score.RawScore,
                               .QueryStart = hsp.Hsp.First.Query.Left,
                               .QueryEnd = hsp.Hsp.Last.Query.Right
                           }
                           Select row).ToArray)
        End Function
    End Module
End Namespace