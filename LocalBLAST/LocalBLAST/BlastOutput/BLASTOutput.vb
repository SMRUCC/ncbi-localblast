Imports System.Web.Script.Serialization
Imports System.Xml.Serialization
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.Views
Imports LANS.SystemsBiology.SequenceModel.FASTA
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic.DocumentFormat.Csv.Extensions
Imports Microsoft.VisualBasic.Text

Namespace LocalBLAST.BLASTOutput

    ''' <summary>
    ''' Blast程序结果对外输出的统一接口类型对象
    ''' </summary>
    ''' <remarks>
    ''' Reader文件夹之下为各种格式的日志文件的读取类对象
    ''' 对于BLAST日志文件，则有一个BlastLogFile对象作为对外保存和其他程序读取的统一接口
    ''' </remarks>
    Public MustInherit Class IBlastOutput : Inherits ITextFile

        Public Const HITS_NOT_FOUND As String = "HITS_NOT_FOUND"

        <XmlIgnore> <ScriptIgnore>
        Public Shadows Property FilePath As String
            Get
                Return MyBase.FilePath
            End Get
            Set(value As String)
                MyBase.FilePath = value
            End Set
        End Property

        Public MustOverride Function Grep(Query As TextGrepMethod, Hits As TextGrepMethod) As IBlastOutput
        ''' <summary>
        ''' 仅导出每条记录的第一个最佳匹配的结果
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride Function ExportBestHit(Optional coverage As Double = 0.5, Optional identities_cutoff As Double = 0.15) As LocalBLAST.Application.BBH.BestHit()
        Public MustOverride Function ExportOverview() As Overview
        ''' <summary>
        ''' 导出每条记录中的所有最佳的匹配结果
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride Function ExportAllBestHist(Optional coverage As Double = 0.5, Optional identities_cutoff As Double = 0.15) As LocalBLAST.Application.BBH.BestHit()

        <XmlElement("BlastOutput_db")>
        Public Overridable Property Database As String

        Public Overrides Function ToString() As String
            Return FilePath
        End Function

        Public MustOverride Function CheckIntegrity(QuerySource As FastaFile) As Boolean
    End Class

    Namespace Views

        ''' <summary>
        ''' 方便程序调试的一个对象数据结构
        ''' </summary>
        ''' <remarks></remarks>
        Public Class Overview

            <XmlElement> Public Property Queries As Query()

            Public Function ExportParalogs() As NCBI.Extensions.LocalBLAST.Application.BBH.BestHit()
                Dim bh = Me.ExportAllBestHist() '符合最佳条件，但是不是自身的记录都是旁系同源
                bh = (From besthit As Application.BBH.BestHit
                      In bh.AsParallel
                      Where Not String.Equals(besthit.QueryName, besthit.HitName, StringComparison.OrdinalIgnoreCase)
                      Select besthit).ToArray
                Return bh
            End Function

            Public Function GetExcelData() As NCBI.Extensions.LocalBLAST.Application.BBH.BestHit()
                Dim LQuery = (From query As Query In Queries Select query.Hits).ToArray.MatrixToVector
                Return LQuery
            End Function

            Public Shared Function LoadExcel(path As String) As Overview
                Dim Excel = path.LoadCsv(Of NCBI.Extensions.LocalBLAST.Application.BBH.BestHit)(False)
                Dim LQuery = (From besthit As Application.BBH.BestHit
                              In Excel
                              Select besthit
                              Group By besthit.QueryName Into Group).ToArray
                Dim lstQuery As Query() = (From queryEntry In LQuery
                                           Let queryData As Query = New Query With {
                                               .UniqueId = queryEntry.QueryName,
                                               .Hits = queryEntry.Group.ToArray
                                           }
                                           Select queryData).ToArray
                Dim Overview As Overview = New Overview With {
                    .Queries = lstQuery
                }
                Return Overview
            End Function

            Public Function ExportAllBestHist(Optional identities As Double = 0.15) As NCBI.Extensions.LocalBLAST.Application.BBH.BestHit()
                Dim LQuery = (From besthit As Application.BBH.BestHit
                              In GetExcelData.AsParallel
                              Where besthit.IsMatchedBesthit(identities)
                              Select besthit).ToArray
                Return LQuery
            End Function

            Public Function ExportBestHit(Optional identities As Double = 0.15) As NCBI.Extensions.LocalBLAST.Application.BBH.BestHit()
                Dim LQuery = (From queryEntry As Query
                              In Queries.AsParallel
                              Let besthit As Application.BBH.BestHit = (From hit As Application.BBH.BestHit
                                                                        In queryEntry.Hits
                                                                        Where hit.IsMatchedBesthit(identities)
                                                                        Select hit).FirstOrDefault
                              Select If(besthit Is Nothing,
                                  New NCBI.Extensions.LocalBLAST.Application.BBH.BestHit With {
                                        .QueryName = queryEntry.UniqueId,
                                        .HitName = NCBI.Extensions.LocalBLAST.BLASTOutput.IBlastOutput.HITS_NOT_FOUND},
                                  besthit)).ToArray
                Return LQuery
            End Function
        End Class

        Public Class Query
            <XmlAttribute> Public Property UniqueId As String
            <XmlElement> Public Property Hits As NCBI.Extensions.LocalBLAST.Application.BBH.BestHit()
        End Class
    End Namespace
End Namespace