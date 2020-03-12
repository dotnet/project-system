' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AutoAddImportsExtensionCollisionDialog
        Private _lastFocus As Control
        Private ReadOnly _helpCallback As IVBAddImportsDialogHelpCallback

        Public Sub New([namespace] As String, identifier As String, minimallyQualifiedName As String, helpCallback As IVBAddImportsDialogHelpCallback, isp As IServiceProvider)
            MyBase.New(isp)
            SuspendLayout()
            Try
                _helpCallback = helpCallback
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
            If _helpCallback IsNot Nothing Then
                _helpCallback.InvokeHelp()
            End If
        End Sub
    End Class
End Namespace
