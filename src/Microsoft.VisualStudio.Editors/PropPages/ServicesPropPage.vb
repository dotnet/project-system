' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Diagnostics.CodeAnalysis
Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports System.Xml
Imports Microsoft.VisualStudio.Editors.Common


Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Friend Class ServicesPropPage
        Inherits PropPageUserControlBase

        Private _ignoreCheckedChanged As Boolean
        Private _ignoreLostFocus As Boolean
        Private _appConfigError As Boolean
        Private _frameworkVersionNumber As UInteger
        Private Const s_requiredFrameworkVersion As UInteger = &H30005

        Public Sub New()
            InitializeComponent()
            SetLocalizedProperties()
            'Loaded handler will call EnsureXmlUpToDate()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False
        End Sub

        Private _currentAppConfigDocument As XmlDocument
        Private _alreadyLoaded As Boolean
        Private _inEnsureXmlUpToDate As Boolean

        Private Sub EnsureXmlUpToDate()
            If _inEnsureXmlUpToDate Then Exit Sub

            Try
                _inEnsureXmlUpToDate = True
                _appConfigError = False
                Dim newDoc As XmlDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(PropertyPageSite, ProjectHierarchy, False)
                'We want to change the document & properties if stuff has changed.  If both documents are null, Object.Equals will return true and
                'we don't need to update.  Other than that, we want to update if one of the documents are null or if neither is and their xml differs.
                If Not _alreadyLoaded OrElse (Not Object.Equals(newDoc, CurrentAppConfigDocument) AndAlso (newDoc Is Nothing OrElse CurrentAppConfigDocument Is Nothing OrElse
                        newDoc.OuterXml <> CurrentAppConfigDocument.OuterXml)) Then
                    _alreadyLoaded = True
                    CurrentAppConfigDocument = newDoc
                    If Not _appConfigError Then
                        'If the application is targetting earlier than .NET Framework 3.5 or a client subset of .NET Framework, then disable this tab.
                        If _frameworkVersionNumber < s_requiredFrameworkVersion OrElse Utils.IsClientFrameworkSubset(ProjectHierarchy) Then
                            SetControlsEnabledProperty(False)
                            EnableApplicationServices.Enabled = False
                        Else
                            Dim servicesEnabled As Boolean = ServicesPropPageAppConfigHelper.ApplicationServicesAreEnabled(CurrentAppConfigDocument, ProjectHierarchy)
                            SetControlsProperties(servicesEnabled)
                        End If
                    End If
                End If

            Finally
                _inEnsureXmlUpToDate = False
            End Try
        End Sub

        Private Property CurrentAppConfigDocument() As XmlDocument
            Get
                Return _currentAppConfigDocument
            End Get
            Set(ByVal value As XmlDocument)
                _currentAppConfigDocument = value
                If value IsNot Nothing Then SetApplicationServicesEnabled(ServicesPropPageAppConfigHelper.ApplicationServicesAreEnabled(CurrentAppConfigDocument, ProjectHierarchy))
            End Set
        End Property

        Private Sub SetLocalizedProperties()
            Dim nonlabelText As String = SR.GetString(SR.PPG_Services_HelpLabelText)
            Dim labelText As String = SR.GetString(SR.PPG_Services_HelpLabelLink)
            HelpLabel.Text = nonlabelText & labelText
            HelpLabel.LinkArea = New System.Windows.Forms.LinkArea(nonlabelText.Length, labelText.Length)
        End Sub

        Private Sub EnableApplicationServices_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EnableApplicationServices.CheckedChanged
            If _ignoreCheckedChanged Then
                _ignoreCheckedChanged = False
                Exit Sub
            End If

            _ignoreLostFocus = True

            'DevDiv Bugs 88577, If the user isn't targetting V3.5 or above, bring up
            'an error stating that the functionality is only available for 3.5 or greater.
            'Also, uncheck the checkbox
            If EnableApplicationServices.Checked AndAlso _frameworkVersionNumber < s_requiredFrameworkVersion Then
                DesignerFramework.DesignerMessageBox.Show(ServiceProvider, SR.GetString(SR.PPG_Services_VersionWarning), Nothing, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                _ignoreCheckedChanged = True
                EnableApplicationServices.Checked = False
            ElseIf Not EnableApplicationServices.Checked Then
                Dim result As DialogResult = DesignerFramework.DesignerMessageBox.Show(ServiceProvider,
                    SR.GetString(SR.PPG_Services_ConfirmRemoveServices), SR.GetString(SR.PPG_Services_ConfirmRemoveServices_Caption),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
                If result = DialogResult.Cancel Then
                    _ignoreCheckedChanged = True
                    EnableApplicationServices.Checked = True
                End If
            End If

            SetApplicationServicesEnabled(EnableApplicationServices.Checked)
            _ignoreLostFocus = False
        End Sub

        Private Sub SetApplicationServicesEnabled(ByVal enable As Boolean)
            'If there's no app.config and we wanted to disable, we're done here.
            If CurrentAppConfigDocument Is Nothing AndAlso Not enable Then Return

            SetWaitCursor(True)
            Try
                Dim initialText As String = Nothing
                Dim finalText As String = Nothing
                If CurrentAppConfigDocument IsNot Nothing Then
                    initialText = CurrentAppConfigDocument.OuterXml
                Else
                    'Try to create the app config document
                    Try
                        CurrentAppConfigDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(PropertyPageSite, ProjectHierarchy, True)
                    Catch ex As CheckoutException
                        'Could not check the file out so we couldn't get an app config document
                        'Undo the user's selection
                        _ignoreCheckedChanged = True
                        EnableApplicationServices.Checked = Not EnableApplicationServices.Checked
                        Return
                    End Try
                End If

                If CurrentAppConfigDocument Is Nothing Then
                    'By now you should have the app config document.  If you don't, something's
                    'seriously wrong...
                    _appConfigError = True
                    SetControlsProperties(False)
                    'NOTE: when the messagebox returns, we'll re-read app config and see if it's better.
                    'If we didn't want to do this, we'd set ignoreCheckedChanged to true before the call
                    'and false afterward.
                    Dim ex As New XmlException(SR.GetString(SR.PPG_Services_InvalidAppConfigXml))
                    DesignerFramework.DesignerMessageBox.Show(CType(ServiceProvider, IServiceProvider), "", ex, DesignerFramework.DesignUtil.GetDefaultCaption(Site))
                Else
                    ServicesPropPageAppConfigHelper.EnsureApplicationServicesEnabled(CurrentAppConfigDocument, enable, ProjectHierarchy)
                    If enable Then AddProjectReferences()
                    If CurrentAppConfigDocument IsNot Nothing Then finalText = CurrentAppConfigDocument.OuterXml
                    SetControlsProperties(enable)
                    If Not String.Equals(initialText, finalText) Then WriteXml()
                End If
            Finally
                SetWaitCursor(False)
            End Try
        End Sub

        Private Sub SetWaitCursor(waiting As Boolean)
            If waiting Then
                Me.Cursor = Cursors.WaitCursor
                Cursor.Current = Cursors.WaitCursor
            Else
                Me.Cursor = Cursors.Default
                Cursor.Current = Cursors.Default
            End If
        End Sub

        Private Sub WriteXml()
            _ignoreLostFocus = True
            Dim writtenSuccessfully As Boolean = ServicesPropPageAppConfigHelper.TryWriteXml(CurrentAppConfigDocument, CType(ServiceProvider, IServiceProvider), ProjectHierarchy)
            _ignoreLostFocus = False

            If Not writtenSuccessfully Then
                EnsureXmlUpToDate()
            End If
        End Sub

        Private Sub SetControlsProperties(ByVal enable As Boolean)
            _ignoreCheckedChanged = True
            EnableApplicationServices.Enabled = Not _appConfigError
            If enable Then
                EnableApplicationServices.Checked = True
                Try
                    AuthenticationServiceUrl.Text = ServicesPropPageAppConfigHelper.AuthenticationServiceHost(CurrentAppConfigDocument, ProjectHierarchy)
                    RolesServiceUrl.Text = ServicesPropPageAppConfigHelper.RolesServiceHost(CurrentAppConfigDocument, ProjectHierarchy)
                    WebSettingsUrl.Text = ServicesPropPageAppConfigHelper.WebSettingsHost(CurrentAppConfigDocument)
                    If ServicesPropPageAppConfigHelper.WindowsAuthSelected(CurrentAppConfigDocument, ProjectHierarchy) Then
                        WindowsBasedAuth.Checked = True
                    Else
                        FormBasedAuth.Checked = True
                    End If
                    CustomCredentialProviderType.Text = ServicesPropPageAppConfigHelper.CustomCredentialProviderType(CurrentAppConfigDocument, ProjectHierarchy)
                Catch ex As InvalidOperationException
                    _appConfigError = True
                    SetControlsProperties(False)
                    Dim xmlException As New XmlException(SR.GetString(SR.PPG_Services_InvalidUrls))
                    DesignerFramework.DesignerMessageBox.Show(CType(ServiceProvider, IServiceProvider), "", xmlException, DesignerFramework.DesignUtil.GetDefaultCaption(Site))
                    Exit Sub
                End Try
            Else
                EnableApplicationServices.Checked = False
                AuthenticationServiceUrl.Text = String.Empty
                RolesServiceUrl.Text = String.Empty
                WebSettingsUrl.Text = String.Empty
                FormBasedAuth.Checked = True
                CustomCredentialProviderType.Text = String.Empty
            End If
            SetControlsEnabledProperty(enable)
            _ignoreCheckedChanged = False
        End Sub

        Private Sub SetControlsEnabledProperty(ByVal shouldEnable As Boolean)
            AuthenticationProviderGroupBox.Enabled = shouldEnable
            RolesServiceUrl.Enabled = shouldEnable
            RolesServiceUrlLabel.Enabled = shouldEnable
            WebSettingsUrl.Enabled = shouldEnable
            WebSettingsUrlLabel.Enabled = shouldEnable
            AdvancedSettings.Enabled = shouldEnable
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropServices
        End Function

        Private Sub AdvancedSettings_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AdvancedSettings.Click
            ShowChildPage(SR.GetString(SR.PPG_ServicesAdvancedPage_Title), GetType(AdvancedServicesDialog))
        End Sub

        Private Sub UrlTextBox_Validating(ByVal sender As Object, ByVal e As CancelEventArgs) Handles RolesServiceUrl.Validating, AuthenticationServiceUrl.Validating, WebSettingsUrl.Validating
            Dim textBox As TextBox = CType(sender, TextBox)
            If textBox.Text Is Nothing Then Exit Sub
            'Since we're interested in the extension, we'll lowercase, just for validation purposes.
            Dim textToValidate As String = textBox.Text.Trim().ToLowerInvariant()
            If textToValidate = "" Then Exit Sub
            Dim badUri As Boolean
            Dim index As Integer = textToValidate.LastIndexOf(".")
            If index <> -1 Then
                Dim invalidExtensions() As String = {".asmx", ".axd", ".svc"}
                For Each extension As String In invalidExtensions
                    If textToValidate.EndsWith(extension) Then
                        badUri = True
                        Exit For
                    End If
                Next
            End If

            If badUri Or Not Uri.IsWellFormedUriString(textToValidate, UriKind.Absolute) Then
                'We get extra Leave events in property pages.  If we don't reset the text, we can get caught in an infinite loop of Leaves and Validating.
                textBox.Text = ""
                'Showing a messagebox in the middle of canceling validation is bad mojo
                Me.BeginInvoke(New MethodInvoker(AddressOf ShowInvalidUrlError))
                e.Cancel = True
            End If
        End Sub

        Public Sub ShowInvalidUrlError()
            _ignoreLostFocus = True
            Dim ex As New XmlException(SR.GetString(SR.PPG_Services_InvalidUrl))
            DesignerFramework.DesignerMessageBox.Show(CType(ServiceProvider, IServiceProvider), "", ex, DesignerFramework.DesignUtil.GetDefaultCaption(Site))
            _ignoreLostFocus = False
        End Sub

        Private Sub RolesServiceUrl_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles RolesServiceUrl.Validated
            If CurrentAppConfigDocument IsNot Nothing Then
                If ServicesPropPageAppConfigHelper.SetRoleServiceUri(CurrentAppConfigDocument, RolesServiceUrl.Text, ProjectHierarchy) Then
                    WriteXml()
                End If
            End If
        End Sub

        Private Sub AuthenticationServiceUrl_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles AuthenticationServiceUrl.Validated
            If CurrentAppConfigDocument IsNot Nothing Then
                If ServicesPropPageAppConfigHelper.SetAuthenticationServiceUri(CurrentAppConfigDocument, AuthenticationServiceUrl.Text, ProjectHierarchy) Then
                    WriteXml()
                End If
            End If
        End Sub

        Private Sub CustomCredentialProviderType_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles CustomCredentialProviderType.Validated
            If CurrentAppConfigDocument IsNot Nothing Then
                If ServicesPropPageAppConfigHelper.SetCustomCredentialProviderType(CurrentAppConfigDocument, CustomCredentialProviderType.Text, ProjectHierarchy) Then
                    WriteXml()
                End If
            End If
        End Sub

        Private Sub WebSettingsUrl_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles WebSettingsUrl.Validated
            If CurrentAppConfigDocument IsNot Nothing Then
                If ServicesPropPageAppConfigHelper.SetAppServicesServiceUri(CurrentAppConfigDocument, WebSettingsUrl.Text) Then
                    WriteXml()
                End If
            End If
        End Sub


        Private Sub WindowsBasedAuth_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles WindowsBasedAuth.CheckedChanged
            'DevDiv Bugs 100690, disable Authentication service location and credential type
            'if Windows auth is selected
            AuthenticationServiceUrl.Enabled = Not WindowsBasedAuth.Checked
            CustomCredentialProviderType.Enabled = Not WindowsBasedAuth.Checked

            If WindowsBasedAuth.Checked And WindowsBasedAuth.Enabled Then
                UpdateMembershipDefaultProviderToWindows(True)
            End If
        End Sub

        Private Sub FormBasedAuth_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles WindowsBasedAuth.CheckedChanged
            If FormBasedAuth.Checked And FormBasedAuth.Enabled Then
                UpdateMembershipDefaultProviderToWindows(False)
            End If
        End Sub

        Private Sub UpdateMembershipDefaultProviderToWindows(ByVal changeToWindows As Boolean)
            If CurrentAppConfigDocument IsNot Nothing Then
                If ServicesPropPageAppConfigHelper.SetMembershipDefaultProvider(CurrentAppConfigDocument, changeToWindows, ProjectHierarchy) Then
                    WriteXml()
                End If
            End If
        End Sub

        Private Sub Loaded(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Load
            _frameworkVersionNumber = Utils.GetProjectTargetFrameworkVersion(ProjectHierarchy)
            EnsureXmlUpToDate()
        End Sub

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Private Sub InvokeHelp()
            Try
                Dim sp As IServiceProvider = ServiceProvider
                If sp IsNot Nothing Then
                    Dim vshelp As VSHelp.Help = CType(sp.GetService(GetType(VSHelp.Help)), VSHelp.Help)
                    vshelp.DisplayTopicFromF1Keyword(GetF1HelpKeyword)
                Else
                    System.Diagnostics.Debug.Fail("Can not find ServiceProvider")
                End If

            Catch ex As System.Exception
                System.Diagnostics.Debug.Fail("Unexpected exception during Help invocation " + ex.Message)
            End Try
        End Sub

        Private Sub HelpLabel_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles HelpLabel.LinkClicked
            InvokeHelp()
        End Sub

        Private Sub ValidateWhenHidden(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
            If Not Me.Visible Then
                Me.Validate()
            End If
        End Sub

        Private Sub ValidateWhenLostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AuthenticationServiceUrl.LostFocus, CustomCredentialProviderType.LostFocus, RolesServiceUrl.LostFocus, WebSettingsUrl.LostFocus
            Me.Validate()
        End Sub

        Private Sub UpdateXmlDocWhenLostFocus(ByVal sender As Object, ByVal e As EventArgs) Handles Me.LostFocus
            If Not _ignoreLostFocus Then EnsureXmlUpToDate()
        End Sub

        Private Sub AddProjectReferences()
            Dim currentProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            currentProject.References.Add("System.Web.Extensions")
            currentProject.References.Add("System.Configuration")
        End Sub
    End Class
End Namespace
