' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AutoAddImportsExtensionCollisionDialog
        Private _lastFocus As Control
        Private _helpCallBack As IVBAddImportsDialogHelpCallback

        Public Sub New([namespace] As String, identifier As String, minimallyQualifiedName As String, helpCAllback As IVBAddImportsDialogHelpCallback, isp As IServiceProvider)
            MyBase.New(isp)
            SuspendLayout()
            Try
                _helpCallBack = helpCAllback
                InitializeComponent()
                txtMain_.Text = String.Format(My.Resources.AddImports.AddImportsExtensionMethodsMainFormatString, [namespace], identifier, minimallyQualifiedName)
                txtMain_.AutoSize = True

                pnlLayout_.AutoSize = True
                AutoSize = True
            Finally
                ResumeLayout()
            End Try
        End Sub

        Private Sub ButtonClick(sender As Object, e As EventArgs) Handles btnOk_.Click, btnCancel_.Click
            Close()
        End Sub

        Private Function GetTextBoxWidth() As Integer
            Return CInt(pnlLayout_.Width - (pnlLayout_.ColumnStyles(0).Width + pnlLayout_.ColumnStyles(4).Width))
        End Function

        Private Sub ButtonGotFocus(sender As Object, e As EventArgs) Handles btnOk_.GotFocus, btnCancel_.GotFocus
            _lastFocus = CType(sender, Control)
        End Sub

        Private Sub LabelGotFocus(sender As Object, e As EventArgs) Handles txtMain_.GotFocus
            _lastFocus.Focus()
        End Sub

        Private Sub ClickHelpButton(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            e.Cancel = True
            OnHelpRequested(New HelpEventArgs(Point.Empty))
        End Sub

        Private Sub RequestHelp(sender As Object, hlpevent As HelpEventArgs) Handles Me.HelpRequested
            If (_helpCallBack IsNot Nothing) Then
                _helpCallBack.InvokeHelp()
            End If
        End Sub
    End Class
End Namespace