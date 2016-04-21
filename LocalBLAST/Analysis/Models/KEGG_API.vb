Imports System.Runtime.CompilerServices
Imports LANS.SystemsBiology.Assembly.KEGG.DBGET.bGetObject
Imports Microsoft.VisualBasic.Language

Namespace Analysis

    ''' <summary>
    ''' KEGG SSDB API
    ''' </summary>
    Public Module KEGG_API

        Public Function EXPORT(source As IEnumerable(Of SSDB.OrthologREST)) As BestHit
            Dim result As New BestHit With {
                .sp = source.First.KEGG_ID.Split(":"c).First,
                .hits = LinqAPI.Exec(Of HitCollection) <= From query As SSDB.OrthologREST
                                                          In source
                                                          Select KEGG_API.Export(query)
            }
            Return result
        End Function

        <Extension> Public Function Export(source As SSDB.OrthologREST) As HitCollection
            If source.Orthologs.IsNullOrEmpty Then
                Return New HitCollection With {
                    .Description = source.Definition,
                    .QueryName = source.KEGG_ID.Split(":"c).Last
                }
            End If
            Dim hits As New HitCollection With {
                .Description = source.Definition,
                .QueryName = source.KEGG_ID.Split(":"c).Last,
                .Hits = LinqAPI.Exec(Of Hit) <= From x As SSDB.SShit
                                                In source.Orthologs
                                                Select x.__export
            }
            Return hits
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

        <Extension>
        Private Function __export(kegg As SSDB.SShit) As Hit
            Return New Hit With {
                .HitName = kegg.Entry.LocusId,
                .tag = kegg.Entry.SpeciesId,
                .Identities = Val(kegg.Identity),
                .Positive = Val(kegg.Identity)
            }
        End Function
    End Module
End Namespace