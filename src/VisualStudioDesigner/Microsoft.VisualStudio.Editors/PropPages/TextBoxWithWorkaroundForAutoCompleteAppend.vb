Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class TextBoxWithWorkaroundForAutoCompleteAppend
        Inherits TextBox

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean

            ' WORKAROUND: See: https://github.com/dotnet/roslyn/issues/7894
            ' Shell has a bug where it clears the text box on CTRL+A when Append is turned on
            ' Prevent it from seeing CTRL+A, and instead handle SelectAll ourselves
            If keyData = (Keys.Control Or Keys.A) Then
                SelectAll()
                Return True
            End If

            Return MyBase.ProcessCmdKey(msg, keyData)

        End Function

    End Class

End Namespace

