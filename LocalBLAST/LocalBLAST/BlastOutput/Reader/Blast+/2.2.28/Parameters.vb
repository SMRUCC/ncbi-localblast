Imports System.Text.RegularExpressions

Namespace LocalBLAST.BLASTOutput.BlastPlus

    Public Structure ParameterSummaryF
        Dim Database, PostedDate As String
        ''' <summary>
        ''' Number of letters in database
        ''' </summary>
        ''' <remarks></remarks>
        Dim Charts As String
        Dim SequenceCounts As String

        Dim Matrix As String
        Dim GapPenaltiesExistence, GapPenaltiesExtension As Double
        ''' <summary>
        ''' Neighboring words threshold
        ''' </summary>
        ''' <remarks></remarks>
        Dim NWThreshold As Double
        ''' <summary>
        ''' Window for multiple hits
        ''' </summary>
        ''' <remarks></remarks>
        Dim Window As Double

        Public Shared Function TryParse(Text As String) As ParameterSummaryF
            Dim Database As String = Mid(Text.Match("Database[:].+$", RegexOptions.Multiline), 11).Trim
            Dim PostedDate As String = Mid(Text.Match("Posted date[:].+$", RegexOptions.Multiline), 15).Trim
            Dim Charts As String = Mid(Text.Match("Number of letters in database[:].+$", RegexOptions.Multiline), 31).Trim
            Dim SequenceCounts As String = Mid(Text.Match("Number of sequences in database[:].+$", RegexOptions.Multiline), 33).Trim
            Dim Matrix As String = Mid(Text.Match("Matrix[:].+$", RegexOptions.Multiline), 9).Trim
            Dim GapPenaltiesExistence As Double = Text.Match("Existence[:]\s*\d+[,]").ParseDouble
            Dim GapPenaltiesExtension As Double = Text.Match(", Extension[:]\s*\d+").ParseDouble
            Dim NWThreshold As Double = Text.Match("Neighboring words threshold[:]\s*\d+").ParseDouble
            Dim Window As Double = Text.Match("Window for multiple hits[:]\s*\d+").ParseDouble

            Return New ParameterSummaryF With {
                .Database = Database,
                .Charts = Charts,
                .GapPenaltiesExistence = GapPenaltiesExistence,
                .GapPenaltiesExtension = GapPenaltiesExtension,
                .Matrix = Matrix,
                .NWThreshold = NWThreshold,
                .PostedDate = PostedDate,
                .SequenceCounts = SequenceCounts,
                .Window = Window
            }
        End Function
    End Structure
End Namespace