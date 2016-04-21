﻿Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.DocumentFormat.Csv

Namespace Analysis

    ''' <summary>
    ''' A collection of hits for the target query protein.
    ''' </summary>
    ''' <remarks>
    ''' 其实这个就是相当于一个KEGG里面的SSDB BBH结果文件
    ''' </remarks>
    Public Class HitCollection : Implements sIdEnumerable

        Public Function Take(IDList As String()) As HitCollection
            Dim LQuery = (From hitData As Hit
                          In Hits
                          Where Array.IndexOf(IDList, hitData.tag) > -1
                          Select hitData).ToArray
            Return New HitCollection With {
                .Hits = LQuery,
                .Description = Description,
                .QueryName = QueryName
            }
        End Function

        ''' <summary>
        ''' The locus tag of the query protein.(主键蛋白质名称)
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property QueryName As String Implements sIdEnumerable.Identifier
        ''' <summary>
        ''' Query protein functional annotation.
        ''' </summary>
        ''' <returns></returns>
        Public Property Description As String
        ''' <summary>
        ''' Query hits protein.
        ''' </summary>
        ''' <returns></returns>
        <XmlElement> Public Property Hits As Hit()
            Get
                Return __hits
            End Get
            Set(value As Hit())
                __hits = value
                If __hits.IsNullOrEmpty Then
                    __hitsHash = New Dictionary(Of Hit)
                    __hits = New Hit() {}
                Else
                    __hitsHash = New Dictionary(Of Hit)(
                        (From x As Hit
                         In value
                         Select x
                         Group x By x.HitName Into Group) _
                              .ToDictionary(Function(x) x.HitName,
                                            Function(x) x.Group.First))
                End If
            End Set
        End Property

        Dim __hits As Hit()
        Dim __hitsHash As Dictionary(Of Hit)

        Public Overrides Function ToString() As String
            Return String.Format("{0}:   {1}", QueryName, Description)
        End Function

        ''' <summary>
        ''' Gets hits protein tag inform by hit protein locus_tag
        ''' </summary>
        ''' <param name="hitName"></param>
        ''' <returns></returns>
        Default Public ReadOnly Property Hit(hitName As String) As Hit
            Get
                If __hitsHash.ContainsKey(hitName) Then
                    Return __hitsHash(hitName)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Function GetHitByTagInfo(tag As String) As Hit
            Dim LQuery = From hit As Hit
                         In Hits
                         Where String.Equals(hit.tag, tag, StringComparison.OrdinalIgnoreCase)
                         Select hit
            Return LQuery.FirstOrDefault
        End Function

        ''' <summary>
        ''' 按照菌株排序
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Friend Function __orderBySp() As HitCollection
            Me.Hits = (From hit As Hit
                       In Me.Hits
                       Select hit
                       Order By hit.tag Ascending).ToArray
            Return Me
        End Function
    End Class

    ''' <summary>
    ''' 和Query的一个比对结果
    ''' </summary>
    Public Class Hit : Implements sIdEnumerable

        ''' <summary>
        ''' <see cref="HitName"></see>所在的物种
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property tag As String
        ''' <summary>
        ''' 和query蛋白质比对上的
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property HitName As String Implements sIdEnumerable.Identifier
        <XmlAttribute> Public Property Identities As Double
        <XmlAttribute> Public Property Positive As Double

        Public Overrides Function ToString() As String
            Return $"[{tag}] {HitName},    Identities:= {Identities};   Positive:= {Positive};"
        End Function
    End Class
End Namespace