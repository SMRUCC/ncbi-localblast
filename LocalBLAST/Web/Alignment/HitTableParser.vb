Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Text

Namespace NCBIBlastResult.WebBlast

    Module HitTableParser

        ''' <summary>
        ''' Document line parser
        ''' </summary>
        ''' <param name="s"></param>
        ''' <returns></returns>
        Public Function Mapper(s As String) As HitRecord
            Dim tokens As String() = s.Split(ASCII.TAB)
            Dim i As VBInteger = Scan0
            Dim hit As New HitRecord With {
                .QueryID = tokens(++i),
                .SubjectIDs = tokens(++i),
                .QueryAccVer = tokens(++i),
                .SubjectAccVer = tokens(++i),
                .Identity = Val(tokens(++i)),
                .AlignmentLength = Val(tokens(++i)),
                .MisMatches = Val(tokens(++i)),
                .GapOpens = Val(tokens(++i)),
                .QueryStart = Val(tokens(++i)),
                .QueryEnd = Val(tokens(++i)),
                .SubjectStart = Val(tokens(++i)),
                .SubjectEnd = Val(tokens(++i)),
                .EValue = Val(tokens(++i)),
                .BitScore = Val(tokens(++i))
            }

            Return hit
        End Function
    End Module
End Namespace