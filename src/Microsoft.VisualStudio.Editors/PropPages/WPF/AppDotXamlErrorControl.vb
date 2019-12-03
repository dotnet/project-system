' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    'UNDONE: help id

    ''' <summary>
    ''' Display an error control with an error icon, a text message, and an Edit Xaml button
    ''' </summary>
    Friend Class AppDotXamlErrorControl

        Public Event EditXamlClicked()

        Public Sub New()
            ' This call is required by the Windows Form Designer.
            InitializeComponent()
        End Sub

        Public Sub New(errorText As String)
            Me.New()
            Me.ErrorText = errorText
        End Sub

        Private Sub EditXamlButton_Click(sender As Object, e As EventArgs) Handles EditXamlButton.Click
            RaiseEvent EditXamlClicked()
        End Sub

        Public Property ErrorText As String
            Get
                Return ErrorControl.Text
            End Get
            Set
                ErrorControl.Text = value
            End Set
        End Property

        Private Sub AppDotXamlErrorControl_Load(sender As Object, e As EventArgs) Handles Me.Load
            TableLayoutPanel1.Width = Math.Max(Width, 400)
            TableLayoutPanel1.Height = ErrorControl.Height + EditXamlButton.Height + 100
        End Sub
    End Class

End Namespace
