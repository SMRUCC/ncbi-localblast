Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.Scripting
Imports LANS.SystemsBiology.ComponentModel.Loci
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.BLASTOutput.BlastPlus.v228
Imports LANS.SystemsBiology.SequenceModel.NucleotideModels

Namespace LocalBLAST.Application

    ''' <summary>
    ''' Blastn Mapping for fastaq
    ''' </summary>
    Public Class BlastnMapping : Inherits Contig

        <Column("Reads.Query")> Public Property ReadQuery As String
        Public Property Reference As String
        Public Property QueryLength As Integer
        <Column("Score(bits)")> Public Property Score As Integer
        <Column("Score(Raw)")> Public Property RawScore As Integer
        <Column("E-value")> Public Property Evalue As Double
        ''' <summary>
        ''' Identities(%)
        ''' </summary>
        ''' <returns></returns>
        <Column("Identities(%)")> Public Property Identities As Double
        <Column("Identities")> Public Property IdentitiesFraction As String
            Get
                Return _identitiesFraction
            End Get
            Set(value As String)
                _identitiesFraction = value
                If Not String.IsNullOrEmpty(value) Then
                    Dim Tokens As String() = value.Replace("\", "/").Split("/"c)
                    If Tokens.Count > 1 Then
                        __identitiesFraction = Math.Abs(Val(Tokens(Scan0) - Val(Tokens(1))))
                    Else
                        __identitiesFraction = Integer.MaxValue
                    End If
                Else
                    __identitiesFraction = Integer.MaxValue
                End If
            End Set
        End Property

        Dim _identitiesFraction As String
        Dim __identitiesFraction As Integer

        ''' <summary>
        ''' Gaps(%)
        ''' </summary>
        ''' <returns></returns>
        <Column("Gaps(%)")> Public Property Gaps As String
        <Column("Gaps")> Public Property GapsFraction As String

#Region "Public Property Strand As String"

        <Ignored> Public ReadOnly Property QueryStrand As Strands
        ''' <summary>
        ''' 在进行装配的时候是以基因组上面的链方向以及位置为基准的
        ''' </summary>
        ''' <returns></returns>
        <Ignored> Public ReadOnly Property ReferenceStrand As Strands

        Dim _strand As String

        Public Property Strand As String
            Get
                Return _strand
            End Get
            Set(value As String)
                _strand = value

                If String.IsNullOrEmpty(value) Then
                    Me._QueryStrand = Strands.Unknown
                    Me._ReferenceStrand = Strands.Unknown
                    Return
                End If

                Dim Tokens As String() = value.Split("/"c)
                Me._QueryStrand = GetStrand(Tokens(Scan0))
                Me._ReferenceStrand = GetStrand(Tokens(1))
            End Set
        End Property
#End Region

        <Column("Left(Query)")> Public Property QueryLeft As Integer
        <Column("Right(Query)")> Public Property QueryRight As Integer
        <Column("Left(Reference)")> Public Property ReferenceLeft As Integer
        <Column("Right(Reference)")> Public Property ReferenceRight As Integer

        'Public Property Lambda As Double
        'Public Property K As Double
        'Public Property H As Double

        '<Column("Lambda(Gapped)")> Public Property Lambda_Gapped As Double
        '<Column("K(Gapped)")> Public Property K_Gapped As Double
        '<Column("H(Gapped)")> Public Property H_Gapped As Double

        '<Column("Effective Search Space")> Public Property EffectiveSearchSpaceUsed As String

        ''' <summary>
        ''' Unique?(这个属性值应该从blastn日志之中导出mapping数据的时候就执行了的)
        ''' </summary>
        ''' <returns></returns>
        <Column("Unique?")> Public Property Unique As Boolean
        <Column("FullLength?")> Public ReadOnly Property AlignmentFullLength As Boolean
            Get
                Return QueryLeft = 1 AndAlso QueryLength = QueryRight
            End Get
        End Property

        ''' <summary>
        ''' Perfect?
        ''' </summary>
        ''' <returns></returns>
        <Column("Perfect?")> Public ReadOnly Property PerfectAlignment As Boolean
            Get
                ' Explicit conditions
                Return (Identities = 100.0R AndAlso __identitiesFraction <= 3) AndAlso Val(Gaps) = 0R
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Me.ReadQuery} //{MappingLocation.ToString}"
        End Function

        ''' <summary>
        ''' 从blastn日志之中导出Mapping的数据
        ''' </summary>
        ''' <param name="Query"></param>
        ''' <returns></returns>
        Public Shared Function CreateObject(Query As Query) As BlastnMapping()
            Dim LQuery = (From hitMapping As SubjectHit
                          In Query.SubjectHits
                          Let blastnHitMapping As BlastnHit =
                              hitMapping.As(Of BlastnHit)
                          Select BlastnMapping.__createObject(Query, blastnHitMapping)).ToArray
            Call setUnique(LQuery)
            Return LQuery
        End Function

        ''' <summary>
        ''' Unique的判断原则：
        ''' 
        ''' 1. 假若一个query之中只含有一个hit，则为unique
        ''' 2. 假若一个query之中含有多个hit的话，假若只有一个hit是perfect类型的，则为unique
        ''' 3. 同一个query之中假若为多个perfect类型的hit的话，则不为unique
        ''' </summary>
        ''' <param name="data"></param>
        ''' <returns></returns>
        Private Shared Function setUnique(ByRef data As BlastnMapping()) As Boolean
            If data.Length = 1 Then
                data(Scan0).Unique = True
                Return True
            End If

            Dim perfects = (From row As BlastnMapping In data
                            Where row.PerfectAlignment
                            Select row).ToArray

            For i As Integer = 0 To data.Length - 1
                data(i).Unique = False
            Next

            If perfects.Length = 0 Then
                Return False
            ElseIf perfects.Length = 1 Then  '只有perfect的被设置为真，其他的已经被设置为false了
                perfects(Scan0).Unique = True
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' 从blastn日志之中导出Mapping的数据
        ''' </summary>
        ''' <param name="Query"></param>
        ''' <param name="hitMapping"></param>
        ''' <returns></returns>
        Private Shared Function __createObject(Query As Query, hitMapping As BlastnHit) As BlastnMapping
            Dim MappingView As New BlastnMapping With {
                .ReadQuery = Query.QueryName,
                .Reference = hitMapping.Name,
                .Evalue = hitMapping.Score.Expect,
                .Gaps = hitMapping.Score.Gaps.Value * 100,
                .GapsFraction = hitMapping.Score.Gaps.FractionExpr,
                .Identities = hitMapping.Score.Identities.Value * 100,
                .IdentitiesFraction = hitMapping.Score.Identities.FractionExpr,
                .QueryLeft = hitMapping.QueryLocation.Left,
                .QueryRight = hitMapping.QueryLocation.Right,
                .RawScore = hitMapping.Score.RawScore,
                .Score = hitMapping.Score.Score,
                .ReferenceLeft = hitMapping.SubjectLocation.Left,
                .ReferenceRight = hitMapping.SubjectLocation.Right,
                .Strand = hitMapping.Strand,
                .QueryLength = Query.QueryLength
            }         '.EffectiveSearchSpaceUsed = Query.EffectiveSearchSpace,
            '.H = Query.p.H,
            '.H_Gapped = Query.Gapped.H,
            '.K = Query.p.K,
            '.K_Gapped = Query.Gapped.K,
            '.Lambda = Query.p.Lambda,
            '.Lambda_Gapped = Query.Gapped.Lambda
            '}
            Return MappingView
        End Function

        ''' <summary>
        ''' 从blastn日志文件之中导出fastq对基因组的比对的结果
        ''' </summary>
        ''' <param name="blastnMapping"></param>
        ''' <returns></returns>
        Public Shared Function Export(blastnMapping As v228) As BlastnMapping()
            Return Export(blastnMapping.Queries)
        End Function

        Public Shared Function Export(lstQuery As Query()) As BlastnMapping()
            Dim LQuery = (From query As Query
                          In lstQuery.AsParallel
                          Select Application.BlastnMapping.CreateObject(query)).ToArray
            Dim ChunkBuffer As BlastnMapping() = LQuery.MatrixToVector
            Return ChunkBuffer
        End Function

        ''' <summary>
        ''' 按照条件 <see cref="BlastnMapping.Unique"/>=TRUE and <see cref="BlastnMapping.PerfectAlignment"/>=TRUE
        ''' 进行可用的alignment mapping结果的筛选
        ''' </summary>
        ''' <returns></returns>
        Public Shared Function TrimAssembly(data As IEnumerable(Of BlastnMapping)) As BlastnMapping()
            Dim sw = Stopwatch.StartNew
            Call $"Start of running {NameOf(TrimAssembly)} action...".__DEBUG_ECHO
            Dim LQuery = (From alignmentReads As BlastnMapping In data.AsParallel
                          Where alignmentReads.Unique AndAlso alignmentReads.PerfectAlignment
                          Select alignmentReads).ToArray
            Call $"[Job DONE!] .....{sw.ElapsedMilliseconds}ms.".__DEBUG_ECHO
            Return LQuery
        End Function

        Protected Overrides Function __getMappingLoci() As NucleotideLocation
            Return New NucleotideLocation(ReferenceLeft, ReferenceRight, ReferenceStrand)
        End Function
    End Class
End Namespace