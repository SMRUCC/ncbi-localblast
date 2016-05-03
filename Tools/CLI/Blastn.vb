Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.DocumentFormat.Csv.DocumentStream.Linq

Partial Module CLI

    <ExportAPI("/Export.Blastn", Usage:="/Export.Blastn /in <in.txt> [/out <out.csv>]")>
    Public Function ExportBlastn(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/in")
        Dim out As String = args.GetValue("/out", inFile.TrimFileExt & ".Csv")

        Using IO As New __writeIO(out)  ' 打开文件流句柄
            Dim IOHandle As Action(Of BlastPlus.Query()) = AddressOf IO.InvokeWrite  ' 获取写文件的IO句柄函数指针
            Call BlastPlus.Transform(inFile, CHUNK_SIZE:=1024 * 1024 * 64, transform:=IOHandle)  ' 执行blast输出大文件分析的并行化查询，内存映射的缓冲块大小为 128GB 的高位内存
        End Using

        Return 0
    End Function

    Private Class __writeIO : Implements System.IDisposable

        ''' <summary>
        ''' 对象序列化串流句柄
        ''' </summary>
        ReadOnly IO As WriteStream(Of BBH.BestHit)

        ''' <summary>
        ''' 打开文件串流句柄
        ''' </summary>
        ''' <param name="handle"></param>
        Sub New(handle As String)
            IO = New WriteStream(Of NCBI.Extensions.LocalBLAST.Application.BBH.BestHit)(handle)
        End Sub

        ''' <summary>
        ''' 执行流写入操作
        ''' </summary>
        ''' <param name="lstQuery"></param>
        Public Sub InvokeWrite(lstQuery As NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus.Query())
            If lstQuery.IsNullOrEmpty Then
                Return
            End If

            Dim outStream = (From x In lstQuery.AsParallel Where Not x.SubjectHits.IsNullOrEmpty Select __creates(x)).MatrixToList

#If DEBUG Then
            If outStream.Count > 0 Then
                Call Console.Write(".")
            End If
#End If

            Call IO.Flush(outStream)
        End Sub

        Private Shared Function __creates(query As BlastPlus.Query) As BBH.BestHit()
            Dim ntHits = (From x As BlastPlus.SubjectHit
                          In query.SubjectHits
                          Select DirectCast(x, BlastPlus.BlastnHit)).ToArray
            Dim outStream = (From x As BlastPlus.BlastnHit
                             In ntHits.AsParallel
                             Select New BBH.BestHit With {
                                 .evalue = x.Score.Expect,
                                 .Score = x.Score.Score,
                                 .HitName = x.Name,
                                 .hit_length = x.Length,
                                 .identities = x.Score.Identities.Value,
                                 .length_hit = x.LengthHit,
                                 .length_hsp = x.SubjectLocation.FragmentSize,
                                 .length_query = x.LengthQuery,
                                 .Positive = x.Score.Positives.Value,
                                 .QueryName = query.QueryName,
                                 .query_length = query.QueryLength}).ToArray
            Return outStream
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    Call IO.Dispose()
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            Me.disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class

    <ExportAPI("/blastn.Query", Usage:="/blastn.Query /query <query.fna> /db <db.DIR> [/evalue 1e-5 /out <out.DIR>]")>
    Public Function BlastnQuery(args As CommandLine.CommandLine) As Integer
        Dim query As String = args("/query")
        Dim DbDIR As String = args("/db")
        Dim evalue As Double = args.GetValue("/evalue", 0.00001)
        Dim outDIR As String = args.GetValue("/out", query.TrimFileExt & ".Blastn/")
        Dim localblast = New LocalBLAST.Programs.BLASTPlus(GCModeller.FileSystem.GetLocalBlast)

        For Each subject As String In FileIO.FileSystem.GetFiles(DbDIR, FileIO.SearchOption.SearchTopLevelOnly, "*.fna", "*.fa", "*.fsa", "*.fasta")
            Dim out As String = outDIR & "/" & IO.Path.GetFileNameWithoutExtension(subject) & ".txt"
            Call localblast.FormatDb(subject, localblast.MolTypeNucleotide).Start(True)
            Call localblast.Blastn(query, subject, out, evalue).Start(True)
        Next

        Return 0
    End Function

    <ExportAPI("/Export.blastnMaps", Usage:="/Export.blastnMaps /in <blastn.txt> [/out <out.csv>]")>
    Public Function ExportBlastnMaps(args As CommandLine.CommandLine) As Integer
        Dim [in] As String = args - "/in"
        Dim out As String = args.GetValue("/out", [in].TrimFileExt & ".Csv")
        Dim blastn = BlastPlus.TryParseBlastnOutput([in])
        Dim maps = BlastnMapping.Export(blastn)
        Return maps.SaveTo(out)
    End Function
End Module
