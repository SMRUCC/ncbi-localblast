Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.DocumentFormat.Csv

Namespace Analysis

    Public Class HitCollection

        Public Function Take(IDList As String()) As HitCollection
            Dim LQuery = (From hitData As Hit
                          In Hits
                          Where Array.IndexOf(IDList, hitData.Tag) > -1
                          Select hitData).ToArray
            Return New HitCollection With {
                .Hits = LQuery,
                .Description = Description,
                .QueryName = QueryName
            }
        End Function

        ''' <summary>
        ''' 主键蛋白质名称
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property QueryName As String
        Public Property Description As String
        <XmlElement> Public Property Hits As Hit()

        Public Overrides Function ToString() As String
            Return String.Format("{0}:   {1}", QueryName, Description)
        End Function

        Default Public ReadOnly Property Hit(hitName As String) As Hit
            Get
                Dim LQuery = From hitEntry As Hit
                             In Hits
                             Where String.Equals(hitEntry.HitName, hitName, StringComparison.OrdinalIgnoreCase)
                             Select hitEntry
                Return LQuery.FirstOrDefault
            End Get
        End Property

        Public Function GetHitByTagInfo(SpeciesTag As String) As Hit
            Dim LQuery = From hit As Hit
                         In Hits
                         Where String.Equals(hit.Tag, SpeciesTag, StringComparison.OrdinalIgnoreCase)
                         Select hit
            Return LQuery.FirstOrDefault
        End Function

        ''' <summary>
        ''' 按照菌株排序
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function Ordered() As HitCollection
            Me.Hits = (From hit As Hit
                       In Me.Hits
                       Select hit
                       Order By hit.Tag Ascending).ToArray
            Return Me
        End Function
    End Class

    Public Class Hit
        ''' <summary>
        ''' <see cref="HitName"></see>所在的物种
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property Tag As String
        ''' <summary>
        ''' 和query蛋白质比对上的
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <XmlAttribute> Public Property HitName As String
        <XmlAttribute> Public Property Identities As Double
        <XmlAttribute> Public Property Positive As Double

        Public Overrides Function ToString() As String
            Return $"[{Tag}] {HitName},    Identities:= {Identities};   Positive:= {Positive};"
        End Function
    End Class
End Namespace