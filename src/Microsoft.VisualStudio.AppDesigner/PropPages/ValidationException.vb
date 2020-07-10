' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The exception will be thrown when validation failed...
    ''' </summary>
    Friend Class ValidationException
        Inherits ApplicationException

        Private ReadOnly _validationResult As ValidationResult
        Private ReadOnly _control As Control

        Public Sub New(result As ValidationResult, message As String, Optional control As Control = Nothing, Optional InnerException As Exception = Nothing)
            MyBase.New(message, InnerException)

            _validationResult = result
            _control = control
        End Sub

        Public ReadOnly Property Result As ValidationResult
            Get
                Return _validationResult
            End Get
        End Property

        Public Sub RestoreFocus()
            If _control IsNot Nothing Then
                _control.Focus()
                Dim textBox = TryCast(_control, TextBox)
                If textBox IsNot Nothing Then
                    textBox.SelectAll()
                End If
            End If
        End Sub
    End Class
End Namespace
