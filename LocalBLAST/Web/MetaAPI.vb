Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.TabularFormat

Namespace NCBIBlastResult

    ''' <summary>
    ''' <see cref="Analysis.BestHit"/> -> <see cref="AlignmentTable"/>
    ''' </summary>
    Public Module MetaAPI

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="bbh"></param>
        ''' <param name="PTT">因为这个是blastp BBH的结果，所以没有基因组的位置信息，在这里使用PTT文档来生成绘图时所需要的位点信息</param>
        ''' <returns></returns>
        Public Function DataParser(bbh As Analysis.BestHit, PTT As PTT) As AlignmentTable

        End Function
    End Module
End Namespace