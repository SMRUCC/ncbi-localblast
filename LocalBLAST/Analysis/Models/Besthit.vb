﻿Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Language
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank

Namespace Analysis

    ''' <summary>
    ''' 元数据Xml文件
    ''' </summary>
    ''' <remarks></remarks>
    Public Class BestHit

        ''' <summary>
        ''' The species name of query.(进行当前匹配操作的物种名称，这个属性不是蛋白质的名称)
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property sp As String
        <XmlElement> Public Property hits As HitCollection()

        Public Function IndexOf(QueryName As String) As Integer
            Dim LQuery = (From hit As HitCollection
                          In hits
                          Where String.Equals(hit.QueryName, QueryName, StringComparison.OrdinalIgnoreCase)
                          Select hit).FirstOrDefault
            If LQuery Is Nothing Then
                Return -1
            Else
                Return Array.IndexOf(hits, LQuery)
            End If
        End Function

        Public Function Take(lstId As String()) As BestHit
            Return New BestHit With {
                .sp = sp,
                .hits = LinqAPI.Exec(Of HitCollection) <=
                        From x As HitCollection
                        In hits.AsParallel
                        Select x.Take(lstId)
            }
        End Function

        Public Function GetTotalIdentities(sp As String) As Double
            Dim LQuery = (From hit As HitCollection
                          In hits
                          Select (From sp_obj As Hit
                                  In hit.Hits
                                  Where String.Equals(sp, sp_obj.tag, StringComparison.OrdinalIgnoreCase)
                                  Select sp_obj.Identities).ToArray).MatrixToList
            If LQuery.IsNullOrEmpty Then
                Return 0
            Else
                Return LQuery.Average
            End If
        End Function

        ''' <summary>
        ''' 从保守的片段数据之中反向取出不保守的片段
        ''' </summary>
        ''' <param name="conserved"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetUnConservedRegions(conserved As IReadOnlyList(Of String())) As String()
            Dim index As List(Of String) = conserved.MatrixToList
            Dim LQuery As String() = (From hit As HitCollection
                                      In Me.hits
                                      Where index.IndexOf(hit.QueryName) = -1
                                      Select hit.QueryName).ToArray
            Return LQuery
        End Function

        Default Public ReadOnly Property Hit(QueryName As String, HitSpecies As String) As String
            Get
                Dim LQuery = (From hitEntry As HitCollection
                              In hits
                              Where String.Equals(hitEntry.QueryName, QueryName)
                              Select hitEntry).FirstOrDefault
                If LQuery Is Nothing Then
                    Return ""
                Else
                    Dim HitData As String = (From hitEntry As Hit
                                             In LQuery.Hits
                                             Where String.Equals(hitEntry.tag, HitSpecies)
                                             Select hitEntry.HitName).FirstOrDefault
                    Return HitData
                End If
            End Get
        End Property

        Default Public ReadOnly Property Hit(QueryName As String) As HitCollection
            Get
                Dim LQuery = From item As HitCollection
                             In hits
                             Where String.Equals(item.QueryName, QueryName, StringComparison.OrdinalIgnoreCase)
                             Select item
                Return LQuery.FirstOrDefault
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return String.Format("{0};  {1} proteins", sp, hits.Count)
        End Function

        ''' <summary>
        ''' 获取能够被比对上的较多数目的物种的编号
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property GetTopHits As String()
            Get
                Dim LQuery = (From hitData As HitCollection In hits Select hitData.Hits).MatrixToList
                Dim Groups = (From hitData As Hit
                              In LQuery
                              Where Not String.IsNullOrEmpty(hitData.HitName)
                              Select hitData
                              Group By hitData.tag Into Group)
                Dim Id As String() = (From Tag In (From bacData
                                                   In Groups
                                                   Where bacData.Group.Count > 0
                                                   Select bacData.Tag,
                                                       n = bacData.Group.Count
                                                   Order By n Descending).ToArray
                                      Select Tag.Tag).ToArray
                Return Id
            End Get
        End Property

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="p">0-1</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function TrimEmpty(p As Double) As BestHit
            Dim LQuery = (From hit As HitCollection In hits Select hit.Hits).MatrixToList
            Dim Grouped = (From hit As Hit
                           In LQuery
                           Where Not String.IsNullOrEmpty(hit.HitName)
                           Select hit
                           Group By hit.tag Into Group).ToArray
            Dim Id As String() = (From hit In Grouped
                                  Where hit.Group.Count >= p * (Grouped.Count - 1)
                                  Select hit.Tag).ToArray
            Dim ChunkBuffer = (From hit As HitCollection
                               In Me.hits
                               Let __hits As Hit() = (From nn As Hit
                                                      In hit.Hits
                                                      Where Array.IndexOf(Id, nn.tag) > -1
                                                      Select nn).ToArray
                               Select hit.InvokeSet(NameOf(hit.Hits), __hits)).ToArray
            Me.hits = ChunkBuffer

            Return Me
        End Function

        ''' <summary>
        ''' 根据比对数据自动的推断出保守的区域
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetConservedRegions(Optional p_cutoff As Double = 0.7, Optional Spacer As Integer = 2) As IReadOnlyList(Of String())
            Dim n As Integer = Me.hits.First.Hits.Length
            Dim p_cut As Double = If(n <= 10, p_cutoff, p_cutoff / n)
            Dim LQuery = (From hit As HitCollection
                          In hits
                          Let p = (From nn As Hit
                                   In hit.Hits
                                   Where Not String.IsNullOrEmpty(nn.HitName)
                                   Select 1).Sum / hit.Hits.Length
                          Select hit.QueryName,
                              IsConserved = p >= p_cut,
                              p).ToArray
            Dim buf As List(Of String()) = New List(Of String())
            Dim i As Integer = 0
            Dim tmps As List(Of String) = New List(Of String)

            Dim __cut = Sub(QueryName As String)      '断裂了
                            Call tmps.Add(QueryName)
                            Call buf.Add(tmps.ToArray)
                            Call tmps.Clear()

                            i = 0
                        End Sub

            For Each x In LQuery

                If Not x.IsConserved Then

                    If i = Spacer Then
                        Call __cut(x.QueryName)
                    ElseIf i = 0 Then '这里的情况是连续的空缺断裂
                        Call __cut(x.QueryName)
                    Else
                        Call tmps.Add(x.QueryName)
                        i += 1
                    End If
                Else
                    Call tmps.Add(x.QueryName)
                    i = 0
                End If
            Next

            Dim DeleteUnConserveds = (From x In LQuery Where Not x.IsConserved Select x.QueryName).ToArray
            buf = (From locus As String()
                   In buf
                   Where locus.Count > 1 OrElse
                       (locus.Count = 1 AndAlso Array.IndexOf(DeleteUnConserveds, locus.First) = -1)
                   Select locus).ToList '删除不保守的位点

            Return buf
        End Function

        ''' <summary>
        ''' 将比对上的物种的fasta文件复制到目标文件夹<paramref name="copyTo"></paramref>之中，目标函数返回所复制的菌株的编号列表
        ''' </summary>
        ''' <param name="source"></param>
        ''' <param name="copyTo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function SelectSourceFromHits(source As String, copyTo As String) As String()
            Dim Entry As Dictionary(Of String, String) = gbExportService.LoadGbkSource(source)
            Dim LQuery = (From hit As HitCollection In hits Select hit.Hits).MatrixToList
            Dim Grouped = (From hit As Hit
                           In LQuery
                           Where Not String.IsNullOrEmpty(hit.HitName)
                           Select hit
                           Group By hit.tag Into Group).ToArray
            Dim list = (From x In Grouped
                        Where x.Group.Count > 0
                        Select x.Tag,
                            x.Group.Count).ToArray

            For Each x In list
                Dim ID As String = x.Tag

                If Entry.ContainsKey(ID) Then
                    Dim path As String = Entry(ID)
                    Dim ext As String = FileIO.FileSystem.GetFileInfo(path).Extension
                    Dim cppath As String = copyTo & "/" & ID & ext
                    Call FileIO.FileSystem.CopyFile(path, cppath, showUI:=FileIO.UIOption.OnlyErrorDialogs, onUserCancel:=FileIO.UICancelOption.ThrowException)
                End If
            Next

            Call list.SaveTo(copyTo & "/Statistics.csv", False)

            Return (From item In list Select item.Tag).ToArray
        End Function

        ''' <summary>
        ''' 按照比对的蛋白质的数目的多少对Hit之中的元素进行统一进行排序
        ''' </summary>
        ''' <param name="TrimNull">将没有任何匹配的对象去除</param>
        ''' <remarks></remarks>
        Public Function InternalSort(TrimNull As Boolean) As List(Of HitCollection)
            Dim SourceLQuery = (From query In (From hit As HitCollection
                                              In Me.hits
                                               Select (From subHit As Hit
                                                      In hit.Hits
                                                       Select QueryName = hit.QueryName,
                                                          Tag = subHit.tag,
                                                          obj = subHit,
                                                          IsHit = Not String.IsNullOrEmpty(subHit.HitName))).MatrixToList
                                Select query
                                Group By query.Tag Into Group).ToArray
            Dim OrderByHits = (From x In SourceLQuery
                               Let order = (From nnn In x.Group.ToArray Where nnn.IsHit Select 1).Count
                               Select dict = x.Group.ToDictionary(Function(obj) obj.QueryName, Function(obj) obj.obj),
                               SpeciesID = x.Tag, order
                               Order By order Descending).ToArray '已经按照比对上的数目排序了
            Dim Ls = New List(Of HitCollection)

            If TrimNull Then
                OrderByHits = (From x In OrderByHits Where x.order > 0 Select x).ToArray
            End If

            For Each Hit As HitCollection In Me.hits
                Dim data = (From x In OrderByHits Select x.dict(Hit.QueryName)).ToArray
                Hit.Hits = data
                Call Ls.Add(Hit)
            Next

            Return Ls
        End Function

        ''' <summary>
        ''' 在这里导出Venn表
        '''
        ''' 格式
        ''' [Description] [QueryProtein]  {[] [HitProtein] [Identities] [Positive]}
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>请注意，为了保持数据之间的一一对应关系，这里不能够再使用并行化了</remarks>
        Public Function ExportCsv(TrimNull As Boolean) As DocumentStream.File
            Dim File As DocumentStream.File = New DocumentStream.File

            '生成表头
            Dim Head As New DocumentStream.RowObject From {"Description", "QueryProtein"}

            hits = InternalSort(TrimNull).ToArray
            hits = (From item In hits Select nnn = item Order By nnn.QueryName Ascending).ToArray  '对Query的蛋白质编号进行排序

            On Error Resume Next

            For Each Hit As Hit In hits.First.Hits
                Call Head.Add("")
                Call Head.Add(Hit.tag)
                Call Head.Add("Identities")
                Call Head.Add("Positive")
            Next

            Call File.Add(Head)

            For Each Hit As HitCollection In hits
                Dim Row = New DocumentStream.RowObject From {Hit.Description, Hit.QueryName}

                For Each HitProtein In Hit.Hits
                    Call Row.Add("")
                    Call Row.Add(HitProtein.HitName)
                    Call Row.Add(HitProtein.Identities)
                    Call Row.Add(HitProtein.Positive)
                Next

                Call File.Add(Row)
            Next

            Return File
        End Function
    End Class
End Namespace