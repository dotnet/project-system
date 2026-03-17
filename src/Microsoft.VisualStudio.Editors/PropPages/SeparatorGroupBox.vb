' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' This class implements a <see cref="GroupBox"/> with custom appearance that
    ''' matches the CPS project properties designer category header style.
    ''' </summary>
    ''' <remarks>
    ''' The control is rendered as a bold, larger label followed by a horizontal line,
    ''' styled to match the new project designer's category separators.
    ''' </remarks>
    Friend NotInheritable Class SeparatorGroupBox
        Inherits GroupBox

        Private Const LabelToLineDistance As Integer = 8
        Private Const HeaderFontSizeIncrease As Single = 3.0F
        Private Const TopPadding As Integer = 4

        Public Sub New()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            ' Use a larger, semi-bold font for the category header to match CPS styling
            Using headerFont As New Font(Font.FontFamily, Font.Size + HeaderFontSizeIncrease, FontStyle.Bold, Font.Unit)
                Dim textPoint = New Point(0, TopPadding)
                TextRenderer.DrawText(e.Graphics, Text, headerFont, textPoint, ForeColor)

                Dim stringSize = TextRenderer.MeasureText(e.Graphics, Text, headerFont, ClientRectangle.Size)
                Dim lineY = TopPadding + (stringSize.Height \ 2)
                Dim linePoint1 = New Point(CInt(stringSize.Width) + LabelToLineDistance, lineY)
                Dim linePoint2 = New Point(ClientRectangle.Right, lineY)

                ' Use a lighter pen color to match CPS's subtle separator line
                Dim lineColor = Color.FromArgb(80, SystemColors.ControlDark)
                Using pen As New Pen(lineColor, 1)
                    e.Graphics.DrawLine(pen, linePoint1, linePoint2)
                End Using
            End Using
        End Sub

    End Class

End Namespace
