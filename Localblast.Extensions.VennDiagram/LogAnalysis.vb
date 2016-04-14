Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic

Namespace BlastAPI

    ''' <summary>
    ''' BLAST日志分析模块
    ''' </summary>
    ''' <remarks>This module its code is too old, obsolete!</remarks>
    Public Module LogAnalysis

        ''' <summary>
        ''' 将多个分析出来的最佳匹配的文件合并成一个文件，这个所得到的文件将会用于文氏图的绘制
        ''' </summary>
        ''' <param name="CsvList"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Merge(CsvList As IEnumerable(Of DocumentStream.File)) As DocumentStream.File
            Dim CsvFile As DocumentStream.File = New DocumentStream.File
            Dim MainIdList As String() = GetMainidList(CsvList)
            Dim LQuery = From Csv In CsvList
                         Select (From row In Csv.AsParallel
                                 Let Pair = New KeyValuePair(Of String, String)(row(0), row(1))
                                 Select Pair
                                 Distinct
                                 Order By Pair.Key Ascending).ToArray '
            Dim DataCollection = LQuery.ToArray

            For i As Integer = 0 To MainIdList.Count - 1
                Dim Id As String = MainIdList(i)
                Dim row As DocumentStream.RowObject = New DocumentStream.RowObject
                row.Add(Id)
                For idx As Integer = 0 To CsvList.Count - 1
                    Dim Query = From k In DataCollection(idx) Where String.Equals(k.Key, Id) Select k '
                    Query = Query.ToArray
                    If Query.Count > 0 Then
                        row.Add(Query.First.Value)
                    Else
                        row.Add("")
                    End If
                Next
                CsvFile.AppendLine(row)
            Next

            Return CsvFile
        End Function

        Private Function GetMainidList(CsvList As IEnumerable(Of DocumentStream.File)) As String()
            Dim List As List(Of String) = New List(Of String)
            For Each Csv In CsvList
                Call List.AddRange(Csv.Column(0))
            Next
            List = (From Id As String In List.AsParallel Select Id Distinct Order By Id Ascending).ToList
            List.Remove("Unknown")

            Return List.ToArray
        End Function

        ''' <summary>
        ''' 从已经分析好的日志文件之中生成最佳匹配表
        ''' </summary>
        ''' <param name="BlastLog1"></param>
        ''' <param name="BlastLog2"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function TakeBestHits(BlastLog1 As NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput, BlastLog2 As NCBI.Extensions.LocalBLAST.BLASTOutput.Standard.BLASTOutput) As DocumentStream.File
            Dim Table1 As DocumentStream.File = BlastLog1.ExportBestHit.ToCsvDoc
            Dim Table2 As DocumentStream.File = BlastLog2.ExportBestHit.ToCsvDoc

            Call Table1.RemoveAt(index:=0)
            Call Table2.RemoveAt(index:=0)

            Dim Query = From row In Table1.AsParallel
                        Let QueryHitPair = __getBestHitPaired(Query:=row, Table:=Table2)
                        Select QueryHitPair
                        Order By QueryHitPair.First Ascending '
            Return Query.ToArray
        End Function

        Private Function __getBestHitPaired(Query As DocumentStream.RowObject, Table As IEnumerable(Of DocumentStream.RowObject)) As DocumentStream.RowObject
            Dim LQuery = From row In Table.AsParallel Where String.Equals(row.Column(1), Query.First) Select row '在表二中查找出目标匹配项
            Dim Result = LQuery.ToArray

            If Result.Count > 0 Then  '找到了对应的项
                If String.Equals(Query.Column(1), Result.First.First) Then '假若两个ID编号相等，则认为是最佳匹配项
                    Return {Query.First, Result.First.First}
                Else
                    Return {Query.First}
                End If
            Else
                Return {Query.First} '不是最佳匹配
            End If
        End Function
    End Module
End Namespace