Imports System.Runtime.CompilerServices
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.BBH
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput
Imports LANS.SystemsBiology.SequenceModel.FASTA

Public Module Extensions

    ReadOnly __ends As String() = {"Matrix:", "Gap Penalties:", "Neighboring words threshold:", "Window for multiple hits:"}

    ''' <summary>
    ''' 根据文件末尾的结束标示来判断这个blast操作是否是已经完成了的
    ''' </summary>
    ''' <param name="path"></param>
    ''' <returns></returns>
    Public Function IsAvailable(path As String) As Boolean
        If Not path.FileExists Then
            Return False
        End If

        Dim i As Integer
        Dim last As String = Tails(path, 2048)

        For Each word As String In __ends
            If InStr(last, word, CompareMethod.Text) > 0 Then
                i += 1
            End If
            If i >= 2 Then
                Return True
            End If
        Next

        Return i >= 2
    End Function

    <Extension> Public Function IsNullOrEmpty(data As IEnumerable(Of BestHit)) As Boolean
        If data.IsNullOrEmpty Then
            Return True
        End If

        Dim LQuery = (From bh In data.AsParallel Where bh.Matched Select bh).ToArray
        Return LQuery.IsNullOrEmpty
    End Function

    ''' <summary>
    ''' Invoke the blastp search for the target protein fasta sequence.(对目标蛋白质序列进行Blastp搜索)
    ''' </summary>
    ''' <param name="Query"></param>
    ''' <param name="Subject"></param>
    ''' <param name="evalue"></param>
    ''' <param name="Blastbin">If the services handler is nothing then the function will construct a new handle automatically.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <Extension> Public Function BlastpSearch(Query As FastaToken, Subject As String,
                                             Optional Evalue As String = "1e-3",
                                             Optional ByRef Blastbin As LocalBLAST.InteropService.InteropService = Nothing) As BlastPlus.v228

        If Not Query.IsProtSource Then
            Call Console.WriteLine("Target fasta sequence file is not a protein sequence data file!")
            Return Nothing
        End If

        Dim TempFile As String = My.Computer.FileSystem.SpecialDirectories.Temp & "/query.tmp"

        If Blastbin Is Nothing Then Blastbin = NCBILocalBlast.CreateSession

        Call Blastbin.FormatDb(Subject, Blastbin.MolTypeProtein).Start(True)
        Call Query.SaveTo(TempFile)
        Call Blastbin.Blastp(TempFile, Subject, My.Computer.FileSystem.SpecialDirectories.Temp & "/blastp.log", Evalue).Start(True)

        Return Blastbin.GetLastLogFile
    End Function

    <Extension> Public Function BlastpSearch(Query As FastaFile, Subject As String,
                                             Optional evalue As String = "1e-3",
                                             Optional ByRef Blastbin As LocalBLAST.InteropService.InteropService = Nothing) As BlastPlus.v228

        Dim TempFile As String = App.AppSystemTemp & "/query.tmp"

        If Blastbin Is Nothing Then Blastbin = NCBILocalBlast.CreateSession

        Call Blastbin.FormatDb(Subject, Blastbin.MolTypeProtein).Start(True)
        Call Query.Save(TempFile)
        Call Blastbin.Blastp(TempFile, Subject, App.GetAppSysTempFile() & "/blastp.log", evalue).Start(True)

        Return Blastbin.GetLastLogFile
    End Function
End Module
