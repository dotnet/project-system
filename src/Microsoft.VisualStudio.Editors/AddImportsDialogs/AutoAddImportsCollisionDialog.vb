' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class AutoAddImportsCollisionDialog
        Private ReadOnly _helpCallBack As IVBAddImportsDialogHelpCallback

        Public Sub New([namespace] As String, identifier As String, minimallyQualifiedName As String, callBack As IVBAddImportsDialogHelpCallback, isp As IServiceProvider)
            MyBase.New(isp)
            _helpCallBack = callBack
            InitializeComponent()
            SuspendLayout()
            Try
                SetNavigationInfo(m_okButton, m_cancelButton, m_rbQualifyCurrentLine)
                SetNavigationInfo(m_cancelButton, m_rbImportsAnyways, m_okButton)
                SetNavigationInfo(m_rbImportsAnyways, m_rbQualifyCurrentLine, m_cancelButton)
                SetNavigationInfo(m_rbQualifyCurrentLine, m_okButton, m_rbImportsAnyways)

                m_lblMain.Text = String.Format(My.Resources.AddImports.AddImportsMainTextFormatString, [namespace], identifier, minimallyQualifiedName)
                m_lblMain.AutoSize = True

                m_lblImportsAnyways.Text = String.Format(My.Resources.AddImports.ImportsAnywaysFormatString, [namespace], [identifier], minimallyQualifiedName)
                m_lblImportsAnyways.AutoSize = True

                m_lblQualifyCurrentLine.Text = String.Format(My.Resources.AddImports.QualifyCurrentLineFormatString, [namespace], [identifier], minimallyQualifiedName)
                m_lblQualifyCurrentLine.AutoSize = True

                m_layoutPanel.AutoSize = True
                AutoSize = True
                ActiveControl = m_okButton
            Finally
                ResumeLayout()
            End Try
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            FixupForRadioButtonLimitations(m_rbImportsAnyways, m_lblImportsAnyways)
            FixupForRadioButtonLimitations(m_rbQualifyCurrentLine, m_lblQualifyCurrentLine)
            Refresh()
        End Sub

        Private Sub FixupForRadioButtonLimitations(radioButtonToLayout As RadioButton, dummyLabel As Label)

            radioButtonToLayout.AutoSize = False
            radioButtonToLayout.Text = dummyLabel.Text

            ' need to add 4 since radiobuttons have default padding of 2px.
            radioButtonToLayout.Height = dummyLabel.Height + 4
            ' Don't set width, that is done by setting the columnspan

            m_layoutPanel.Controls.Remove(dummyLabel)
            m_layoutPanel.SetColumnSpan(radioButtonToLayout, 4) ' will set the Width appropriately
        End Sub

        Private Sub ButtonClick(sender As Object, e As EventArgs) Handles m_cancelButton.Click, m_okButton.Click
            Close()
        End Sub

        Private Sub ClickHelpButton(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            e.Cancel = True
            OnHelpRequested(New HelpEventArgs(Point.Empty))
        End Sub

        Private Sub RequestHelp(sender As Object, hlpevent As HelpEventArgs) Handles Me.HelpRequested
            If _helpCallBack IsNot Nothing Then
                _helpCallBack.InvokeHelp()
            End If
        End Sub

        Public ReadOnly Property ShouldImportAnyways As Boolean
            Get
                Return m_rbImportsAnyways.Checked
            End Get
        End Property
    End Class
End Namespace
