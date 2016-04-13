Imports System.Text.RegularExpressions
Imports System.Xml.Serialization
Imports LANS.SystemsBiology.ComponentModel.Loci
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.ComponentModel

Namespace LocalBLAST.BLASTOutput.BlastPlus

    Public Class SubjectHit

        <XmlAttribute> Public Property Name As String
        ''' <summary>
        ''' hit蛋白质序列的全长
        ''' </summary>
        ''' <returns></returns>
        <XmlAttribute> Public Property Length As Long
        <XmlElement> Public Property Score As Score

        Public Property Hsp As HitSegment()

        ''' <summary>
        ''' Hit position on the query sequence.
        ''' </summary>
        ''' <returns></returns>
        Public Overridable ReadOnly Property QueryLocation As Location
            Get
                Return New Location(Hsp.First.Query.Left, Hsp.Last.Query.Right)
            End Get
        End Property

        Public Overridable ReadOnly Property SubjectLocation As Location
            Get
                Return New Location(Hsp.First.Sbjct.Left, Hsp.Last.Sbjct.Right)
            End Get
        End Property

        ''' <summary>
        ''' 高分区的hit片段的长度
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property LengthHit As Integer
            Get
                Dim LQuery = (From Segment In Hsp Select (From ch As Char In Segment.Query.SequenceData Where ch = "-"c Select 1).Sum).Sum
                Return Score.Gaps.Denominator - LQuery  ' 减去插入的空格就是比对上的长度了
            End Get
        End Property

        Public ReadOnly Property LengthQuery As Integer
            Get
                Dim LQuery = (From Segment In Hsp Select (From ch As Char In Segment.Sbjct.SequenceData Where ch = "-"c Select 1).Sum).Sum
                Return Score.Gaps.Denominator - LQuery
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return String.Format("Name: {0}, Length: {1}", Name, Length)
        End Function

        Public Const NO_HITS_FOUND As String = "No hits found"

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="text"></param>
        ''' <returns></returns>
        ''' <remarks>请不要在这里使用.AsParallel拓展方法，以保持各个片段的顺序关系</remarks>
        Public Shared Function GetItems(text As String) As SubjectHit()
            If InStr(text, NO_HITS_FOUND) Then
                Return New SubjectHit() {}
            End If

            Dim Tokens = Regex.Split(text, "^>", RegexOptions.Multiline).Skip(1).ToArray
            Dim LQuery As SubjectHit() = (From s As String
                                          In Tokens
                                          Select SubjectHit.TryParse(s)).ToArray
            Return LQuery
        End Function

        Protected Const PAIRWISE As String = "Query\s+\d+\s+.+?\s+\d+.+?Sbjct\s+\d+\s+.+?\s+\d+"

        Public Shared Function TryParse(Text As String) As SubjectHit
            Dim name As String = Strings.Split(Text, "Length=").First.TrimA
            Dim Length As Long = CLng(Text.Match("Length=\d+").ParseDouble)
            Dim strHsp As String() = (From Match As Match
                                      In Regex.Matches(Text, PAIRWISE, RegexOptions.Singleline + RegexOptions.IgnoreCase)
                                      Select Match.Value).ToArray
            Dim hit As SubjectHit = New SubjectHit With {
                .Score = Score.TryParse(Of Score)(Text),
                .Name = name,
                .Length = Length,
                .Hsp = ParseHitSegments(strHsp)
            }

            Return hit
        End Function

        Protected Shared Function ParseHitSegments(TextLines As String()) As HitSegment()
            Dim Hsp As HitSegment() = New HitSegment(TextLines.Length - 1) {}

            For i As Integer = 0 To TextLines.Length - 1
                Dim buffer As String() = (From strLine As String
                                          In Strings.Split(TextLines(i), vbLf)
                                          Select strLine.Replace(vbCr, "")).ToArray
                Hsp(i) = HitSegment.TryParse(buffer)
            Next

            Return Hsp
        End Function
    End Class
End Namespace