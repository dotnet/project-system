' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' This class implements a <see cref="GroupBox"/> with custom appearance.
    ''' </summary>
    ''' <remarks>
    ''' The control is rendered as a label followed by a horizontal line, commonly used as a separator.
    ''' </remarks>
    Friend NotInheritable Class SeparatorGroupBox
        Inherits GroupBox

        Private Const LabelToLineDistance As Integer = 8

        Public Sub New()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim format = New StringFormat() With {.Alignment = StringAlignment.Near, .Trimming = StringTrimming.Character}
            TextRenderer.DrawText(e.Graphics, Text, Font, New Point(0), ForeColor)

            Dim linePoint1 = New Point(ClientRectangle.Left, ClientRectangle.Top + (Font.Height \ 2))
            Dim linePoint2 = New Point(ClientRectangle.Right, ClientRectangle.Top + (Font.Height \ 2))

            Dim stringSize = TextRenderer.MeasureText(e.Graphics, Text, Font, ClientRectangle.Size)
            linePoint1.X += CInt(stringSize.Width)
            linePoint1.X += LabelToLineDistance

            Using pen As New Pen(SystemColors.ControlDark, SystemInformation.BorderSize.Height)
                e.Graphics.DrawLine(pen, linePoint1, linePoint2)
            End Using
        End Sub

    End Class

End Namespace
