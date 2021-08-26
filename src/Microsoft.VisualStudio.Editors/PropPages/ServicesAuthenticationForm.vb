' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Friend Class ServicesAuthenticationForm
        Inherits Form

        Private ReadOnly _serviceProvider As IServiceProvider
        Private ReadOnly _authenticationUrl As String

        Public Sub New(authenticationUrl As String, authenticationHost As String, serviceProvider As IServiceProvider)
            InitializeComponent()
            AuthenticationServiceUrl.Text = authenticationHost
            _authenticationUrl = authenticationUrl
            _serviceProvider = serviceProvider
        End Sub

        Public ReadOnly Property AuthenticationUrl As String
            Get
                Return _authenticationUrl
            End Get
        End Property

        Public ReadOnly Property UserName As String
            Get
                Return UserNameTextBox.Text
            End Get
        End Property

        Public ReadOnly Property Password As String
            Get
                Return PasswordTextBox.Text
            End Get
        End Property

        Private Sub ShowHelp()
            Try
                If _serviceProvider IsNot Nothing Then
                    Dim vshelp As VSHelp.Help = CType(_serviceProvider.GetService(GetType(VSHelp.Help)), VSHelp.Help)
                    vshelp.DisplayTopicFromF1Keyword(HelpKeywords.VBProjPropSettingsLogin)
                Else
                    Debug.Fail("Can not find ServiceProvider")
                End If
            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(ShowHelp), NameOf(ServicesAuthenticationForm))
            End Try
        End Sub

        Private Sub ServiceAuthenticationForm_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            ShowHelp()
        End Sub

        Private Sub ServiceAuthenticationForm_HelpRequested(sender As Object, hlpevent As HelpEventArgs) Handles Me.HelpRequested
            ShowHelp()
        End Sub

        Private _loadAnonymous As Boolean
        Public Property LoadAnonymously As Boolean
            Get
                Return _loadAnonymous
            End Get
            Set
                _loadAnonymous = value
            End Set
        End Property

        Private Sub Cancel_Click(sender As Object, e As EventArgs) Handles Cancel.Click
            _loadAnonymous = True
            DialogResult = DialogResult.OK
        End Sub

        Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
            If keyData = Keys.Escape Then
                Close()
            End If

            Return MyBase.ProcessDialogKey(keyData)
        End Function

    End Class
End Namespace
