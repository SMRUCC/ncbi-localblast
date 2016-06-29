#Region "Microsoft.VisualBasic::ea52479d60c212e329f3643567c9911d, ..\localblast\LocalBLAST\LocalBLAST\LocalBLAST\Application\COG\Whog\Category.vb"

' Author:
' 
'       asuka (amethyst.asuka@gcmodeller.org)
'       xieguigang (xie.guigang@live.com)
' 
' Copyright (c) 2016 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
' 
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
' 
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System.Text.RegularExpressions
Imports System.Xml.Serialization
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language

Namespace LocalBLAST.Application.RpsBLAST.Whog

    Public Class Category

        <XmlAttribute> Public Property CategoryId As String
        <XmlAttribute> Public Property COG As String
        <XmlElement> Public Property Description As String

        <XmlElement> Public Property IdList As KeyValuePair()
            Get
                Return _IdList
            End Get
            Set(value As KeyValuePair())
                If value Is Nothing Then
                    Return
                End If

                Dim LQuery As NamedValue(Of String())() =
                    LinqAPI.Exec(Of NamedValue(Of String())) <= From v As KeyValuePair
                                                                In value
                                                                Select New NamedValue(Of String()) With {
                                                                    .Name = v.Key,
                                                                    .x = v.Value.Split
                                                                }
                _IdList = value
                IdTokens = LQuery
                _lstLocus =
                    LinqAPI.Exec(Of String) <= From x As NamedValue(Of String())
                                               In LQuery
                                               Let IdList As String() = x.x
                                               Select IdList
            End Set
        End Property

        Dim _IdList As KeyValuePair()
        Dim IdTokens As NamedValue(Of String())()
        Dim _lstLocus As String()

        Const REGX_CATAGORY As String = "\[[^]]+\]"
        Const REGX_COG_ID As String = "COG\d+"

        Public Overrides Function ToString() As String
            Return String.Format("[{0}] {1} --> {2}", CategoryId, COG, Description)
        End Function

        Public Function ContainsGene(id As String) As Boolean
            Return Array.IndexOf(_lstLocus, id) > -1
        End Function

        Protected Friend Shared Function Parse(srcText As String) As Category
            Dim cat As New Category
            Dim Tokens As String() = Strings.Split(srcText, vbLf)
            Dim description As String = Tokens.First

            cat.CategoryId = Regex.Match(description, REGX_CATAGORY).Value
            cat.CategoryId = Mid(cat.CategoryId, 2, Len(cat.CategoryId) - 2)
            cat.COG = Regex.Match(description, REGX_COG_ID).Value
            cat.Description = Mid(description, Len(cat.CategoryId) + Len(cat.COG) + 4).Trim

            Dim list As New List(Of KeyValuePair)

            For Each line As String In Tokens.Skip(1)
                Dim sss As String() = Strings.Split(line, ":")
                If sss.Length = 2 Then
                    list += New KeyValuePair With {
                        .Key = sss(0).TrimA,
                        .Value = sss(1).Trim
                    }
                Else
                    list.Last.Value &= " " & Trim(line)
                End If
            Next

            cat.IdList = list.ToArray

            Return cat
        End Function

        Public Function Find(Id As String) As String
            If IdTokens Is Nothing Then
                Return ""
            End If

            Dim LQuery As String =
                LinqAPI.DefaultFirst(Of String) <= From item
                                                   In IdTokens
                                                   Where Array.IndexOf(item.x, Id) > -1
                                                   Select item.Name
            Return LQuery
        End Function
    End Class
End Namespace
