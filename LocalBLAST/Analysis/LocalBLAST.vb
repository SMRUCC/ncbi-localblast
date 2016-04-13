Imports Microsoft.VisualBasic
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.InteropService
Imports Microsoft.VisualBasic.ComponentModel

Namespace Analysis

    Public Module LocalBLAST

        Private Function __blast(File1 As String,
                                 File2 As String,
                                 Idx As Integer,
                                 logDIR As String,
                                 LocalBlast As InteropService) As String   '匿名函数返回日志文件名

            Dim LogName As String = GetFileName(File1, File2)
            Dim LogFile As String = String.Format("{0}/{1}__{2}.log", logDIR, Idx, LogName)

            Call Console.WriteLine("[{0}, {1}]", File1, File2)
            Call LocalBlast.Blastp(File1, File2, LogFile, e:="1").Start(WaitForExit:=True) 'performence the BLAST

            Return LogFile
        End Function

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="FileList"></param>
        ''' <param name="LogDIR">默认为桌面</param>
        ''' <returns>日志文件列表</returns>
        ''' <remarks></remarks>
        Public Function BLAST(FileList As String(), LogDIR As String, pBlast As InitializeParameter) As List(Of Pair())
            Dim Files As Comb(Of String) = FileList
            Dim LocalBlast As InteropService = CreateInstance(pBlast)
            Dim DirIndex As Integer = 1
            Dim ReturnedList As List(Of Pair()) = New List(Of Pair())

            For Each File As String In FileList  'formatdb
                Call LocalBlast.FormatDb(File, "").Start(WaitForExit:=True)
            Next

            For Each List In Files.CombList
                Dim Dir As String = String.Format("{0}/{1}/", LogDIR, DirIndex)
                Dim Index As Integer = 1
                Dim LogPairList As List(Of Pair) = New List(Of Pair)

                DirIndex += 1
                Call FileIO.FileSystem.CreateDirectory(directory:=Dir)

                For i As Integer = 0 To List.Count - 1
                    Dim Log1 As String = __blast(List(i).Key, List(i).Value, Index, LogDIR, LocalBlast)
                    Index += 1
                    Dim Log2 As String = __blast(List(i).Value, List(i).Key, Index, LogDIR, LocalBlast)
                    Index += 1
                    LogPairList += New Pair With {.File1 = Log1, .File2 = Log2}
                Next

                Call ReturnedList.Add(LogPairList.ToArray)
            Next

            Return ReturnedList
        End Function

        Private Function GetFileName(File1 As String, File2 As String) As String
            Dim N1 As String = File1.Replace("\", "/").Split(CChar("/")).Last.Split(CChar(".")).First
            Dim N2 As String = File2.Replace("\", "/").Split(CChar("/")).Last.Split(CChar(".")).First
            Return String.Format("{0}_{1}", N1, N2)
        End Function
    End Module
End Namespace