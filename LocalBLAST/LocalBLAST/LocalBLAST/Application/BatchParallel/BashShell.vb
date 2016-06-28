﻿#Region "b6a9115f65b73c35a470e694071a6e3d, ..\LocalBLAST\LocalBLAST\LocalBLAST\Application\BatchParallel\BashShell.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xieguigang (xie.guigang@live.com)
    ' 
    ' Copyright (c) 2016 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System.Text
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData

Namespace LocalBLAST.Application.BatchParallel

    ''' <summary>
    ''' 生成用于linux服务器上面批量运行的blast脚本
    ''' </summary>
    ''' 
    <PackageNamespace("NCBI.LocalBLAST.BashShell")>
    Public Module BashShell

        ''' <summary>
        ''' 2. 保存脚本
        ''' </summary>
        ''' <param name="batch"></param>
        ''' <param name="outDIR"></param>
        ''' <returns></returns>
        ''' 
        <ExportAPI("Bash.Caller.Save")>
        Public Function ScriptCallSave(batch As Generic.IEnumerable(Of String), outDIR As String) As Boolean
            Dim caller As StringBuilder = New StringBuilder("#!/bin/bash" & vbCrLf)
            Dim i As Integer = 1000

            For Each script As String In batch
                Dim path As String = outDIR & "/" & i & ".sh"
                Dim bash As StringBuilder = New StringBuilder("#!/bin/bash" & vbCrLf)

                Call bash.AppendLine(script)
                Call bash.SaveTo(path)
                Call caller.AppendLine("./" & FileIO.FileSystem.GetFileInfo(path).Name & " &")
            Next

            Return caller.SaveTo(outDIR & "/Invoke.sh")
        End Function

        ''' <summary>
        ''' 1. 生成两两比对的脚本调用
        ''' </summary>
        ''' <param name="inDIR"></param>
        ''' <param name="inRefAs">Linux服务器上面的引用位置</param>
        ''' <param name="outDIR"></param>
        ''' <param name="evalue"></param>
        ''' <param name="blastDIR">这个应该是linux服务器上面的位置，包含blastp+makeblastdb</param>
        ''' <returns></returns>
        ''' 
        <ExportAPI("Venn.Batch")>
        Public Function VennBatch(blastDIR As String, inDIR As String, inRefAs As String, outDIR As String, evalue As String) As String()
            Dim fastas As String() =
                FileIO.FileSystem.GetFiles(inDIR,
                                           FileIO.SearchOption.SearchTopLevelOnly,
                                           "*.fasta",
                                           "*.fa",
                                           "*.fsa",
                                           "*.fas").ToArray
            Dim LQuery = (From fa As String
                          In fastas
                          Select Batch(blastDIR,
                              query:=fa,
                              evalue:=evalue,
                              inRefAs:=inRefAs,
                              outDIR:=outDIR,
                              subject:=fastas)).ToArray
            Return LQuery
        End Function

        <ExportAPI("Batch")>
        Public Function Batch(blastDIR As String, query As String, subject As String(), inRefAs As String, outDIR As String, evalue As String) As String
            Dim script As StringBuilder = New StringBuilder
            Dim blastp As String = blastDIR & "/blastp"
            Dim makeblastdb As String = blastDIR & "/makeblastdb"

            For Each sbj As String In subject
                Dim out As String = VennDataBuilder.BuildFileName(query, sbj, outDIR)

                Call script.AppendLine($"{makeblastdb} -dbtype prot -in {sbj.CliPath}")
                Call script.AppendLine($"{blastp} -in {query.CliPath} -db {sbj.CliPath} -evalue {evalue} -out {out.CliPath}")
                Call script.AppendLine(vbCrLf)
            Next

            Return script.ToString
        End Function
    End Module
End Namespace
