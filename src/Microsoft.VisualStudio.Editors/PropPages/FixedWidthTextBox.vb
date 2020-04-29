' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' This is a class to automatically set the height of textbox.
    ''' </summary>
    Friend Class FixedWidthTextBox
        Inherits TextBox

        Protected Overrides Sub OnTextChanged(e As EventArgs)
            CalculateAndSetHeight()
            MyBase.OnTextChanged(e)
        End Sub

        Protected Overrides Sub OnSizeChanged(e As EventArgs)
            CalculateAndSetHeight()
            MyBase.OnSizeChanged(e)
        End Sub

        Private Sub CalculateAndSetHeight()
            Using graphics = CreateGraphics()
                Dim area = graphics.MeasureString(Text, Font, Width)
                Height = CInt(area.Height)
            End Using
        End Sub
    End Class

End Namespace
