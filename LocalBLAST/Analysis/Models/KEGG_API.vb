Imports System.Runtime.CompilerServices
Imports LANS.SystemsBiology.Assembly.KEGG.DBGET.bGetObject
Imports Microsoft.VisualBasic.Language

Namespace Analysis

    ''' <summary>
    ''' KEGG SSDB API
    ''' </summary>
    Public Module KEGG_API

        <Extension> Public Function Export(source As SSDB.OrthologREST) As HitCollection

        End Function

        Public Function Export(source As IEnumerable(Of SSDB.Ortholog), tag As String) As HitCollection
            Dim hits As New HitCollection With {
                .QueryName = tag,
                .Hits = LinqAPI.Exec(Of SSDB.Ortholog, Hit)(source) <= Function(x) KEGG_API.__export(x)
            }
            Return hits
        End Function

        Private Function __export(kegg As SSDB.Ortholog) As Hit
            Return New Hit With {
                .HitName = kegg.hit_name,
                .Identities = kegg.identity,
                .Positive = kegg.identity,
                .tag = kegg.hit_name.Split(":"c).First
            }
        End Function
    End Module
End Namespace