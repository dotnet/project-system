' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics.CodeAnalysis
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Friend Class ServicesAuthenticationForm
        Inherits System.Windows.Forms.Form

        Private _serviceProvider As IServiceProvider
        Private _authenticationUrl As String

        <SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings")>
        Public Sub New(ByVal authenticationUrl As String, ByVal authenticationHost As String, ByVal serviceProvider As IServiceProvider)
            InitializeComponent()
            AuthenticationServiceUrl.Text = authenticationHost
            _authenticationUrl = authenticationUrl
            _serviceProvider = serviceProvider
        End Sub


        <SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")>
        Public ReadOnly Property AuthenticationUrl() As String
            Get
                Return _authenticationUrl
            End Get
        End Property

        Public ReadOnly Property UserName() As String
            Get
                Return UserNameTextBox.Text
            End Get
        End Property

        Public ReadOnly Property Password() As String
            Get
                Return PasswordTextBox.Text
            End Get
        End Property

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Private Sub ShowHelp()
            Try
                If _serviceProvider IsNot Nothing Then
                    Dim vshelp As VSHelp.Help = CType(_serviceProvider.GetService(GetType(VSHelp.Help)), VSHelp.Help)
                    vshelp.DisplayTopicFromF1Keyword(HelpKeywords.VBProjPropSettingsLogin)
                Else
                    System.Diagnostics.Debug.Fail("Can not find ServiceProvider")
                End If
            Catch ex As System.Exception When Common.Utils.ReportWithoutCrash(ex, NameOf(ShowHelp), NameOf(ServicesAuthenticationForm))
            End Try
        End Sub

        Private Sub ServiceAuthenticationForm_HelpButtonClicked(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
            ShowHelp()
        End Sub

        Private Sub ServiceAuthenticationForm_HelpRequested(ByVal sender As System.Object, ByVal hlpevent As System.Windows.Forms.HelpEventArgs) Handles Me.HelpRequested
            ShowHelp()
        End Sub

        Private _loadAnonymous As Boolean
        Public Property LoadAnonymously() As Boolean
            Get
                Return _loadAnonymous
            End Get
            Set(ByVal value As Boolean)
                _loadAnonymous = value
            End Set
        End Property

        Private Sub Cancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel.Click
            _loadAnonymous = True
            Me.DialogResult = System.Windows.Forms.DialogResult.OK
        End Sub

        Protected Overrides Function ProcessDialogKey(ByVal keyData As System.Windows.Forms.Keys) As Boolean
            If keyData = Keys.Escape Then
                Me.Close()
            End If

            Return MyBase.ProcessDialogKey(keyData)
        End Function

    End Class
End Namespace