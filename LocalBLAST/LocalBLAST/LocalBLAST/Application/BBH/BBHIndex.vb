Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.ComponentModel.KeyValuePair
Imports Microsoft.VisualBasic.Serialization

Namespace LocalBLAST.Application.BBH

    ''' <summary>
    ''' 可以使用这个对象来表述<see cref="I_BlastQueryHit"/>的所有派生类
    ''' </summary>
    Public Class BBHIndex : Inherits I_BlastQueryHit
        Implements IKeyValuePair
        Implements IQueryHits

        Public Property identities As Double Implements IQueryHits.identities

        ''' <summary>
        ''' 动态属性
        ''' </summary>
        ''' <returns></returns>
        <Meta(GetType(String))>
        Public Property Properties As Dictionary(Of String, String)
            Get
                If _Properties Is Nothing Then
                    _Properties = New Dictionary(Of String, String)
                End If
                Return _Properties
            End Get
            Set(value As Dictionary(Of String, String))
                _Properties = value
            End Set
        End Property

        Dim _Properties As Dictionary(Of String, String)

        Default Public Property [Property](Name As String) As String
            Get
                If Properties.ContainsKey(Name) Then
                    Return Properties(Name)
                Else
                    Return ""
                End If
            End Get
            Set(value As String)
                If Properties.ContainsKey(Name) Then
                    Call Properties.Remove(Name)
                End If
                Properties.Add(Name, value)
            End Set
        End Property

        <Ignored> Public Property Positive As Double
            Get
                Dim p As String = [Property]("Positive")
                If String.IsNullOrEmpty(p) Then
                    p = [Property]("positive")
                End If
                Return Val(p)
            End Get
            Set(value As Double)
                [Property]("Positive") = CStr(value)
            End Set
        End Property

        Public Overrides Function ToString() As String
            Return Me.GetJson
        End Function
    End Class
End Namespace