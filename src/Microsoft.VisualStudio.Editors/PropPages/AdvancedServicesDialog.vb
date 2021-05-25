' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Xml

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Friend Class AdvancedServicesDialog
        Inherits PropPageUserControlBase

        Private _savedXml As String
        Private _appConfigDocument As XmlDocument

        Protected Overrides Sub PostInitPage()
            Try
                _appConfigDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(PropertyPageSite, ProjectHierarchy, False)
            Catch innerException As XmlException
                Dim ex As New XmlException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_InvalidAppConfigXml)
                DesignerFramework.DesignerMessageBox.Show(CType(ServiceProvider, IServiceProvider), "", ex, DesignerFramework.DesignUtil.GetDefaultCaption(Site))
                Enabled = False
                Return
            End Try

            Enabled = True

            SavePasswordHashLocallyCheckbox.Checked = ServicesPropPageAppConfigHelper.GetSavePasswordHashLocally(_appConfigDocument, ProjectHierarchy)
            Dim honorCookieExpiryValue As Boolean? = ServicesPropPageAppConfigHelper.GetEffectiveHonorCookieExpiry(_appConfigDocument, ProjectHierarchy)
            If honorCookieExpiryValue.HasValue Then
                HonorServerCookieExpirationCheckbox.Checked = CBool(honorCookieExpiryValue)
            Else
                HonorServerCookieExpirationCheckbox.CheckState = System.Windows.Forms.CheckState.Indeterminate
            End If

            AddTimeUnitsToComboBox()
            SetCacheTimeoutControlValues(ServicesPropPageAppConfigHelper.GetCacheTimeout(_appConfigDocument, ProjectHierarchy))
            SetUseCustomConnectionStringControlValues(_appConfigDocument)

            _savedXml = _appConfigDocument.OuterXml
        End Sub

        Public Overrides Sub Apply()
            ServicesPropPageAppConfigHelper.TryWriteXml(_appConfigDocument, CType(ServiceProvider, IServiceProvider), ProjectHierarchy)
            IsDirty = False
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAdvancedServices
        End Function

        Public Sub New()
            InitializeComponent()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False
        End Sub

        Private Sub AddTimeUnitsToComboBox()
            If TimeUnitComboBox.Items.Count = 0 Then
                TimeUnitComboBox.Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Seconds)
                TimeUnitComboBox.Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Minutes)
                TimeUnitComboBox.Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Hours)
                TimeUnitComboBox.Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Days)
            End If
        End Sub

        Private Enum TimeUnit
            Seconds
            Minutes
            Hours
            Days
        End Enum

        Private Sub SetCacheTimeoutControlValues(cacheTimeout As Integer)
            If cacheTimeout < 0 Then cacheTimeout = 0
            If cacheTimeout > Integer.MaxValue Then cacheTimeout = Integer.MaxValue

            'The cache timeout value is in seconds.  
            Dim unit As TimeUnit = TimeUnit.Seconds

            If cacheTimeout <> 0 Then
                'Let's see whether we should display this as minutes, which we'll do if we have an even number of minutes...
                If cacheTimeout Mod 60 = 0 Then
                    cacheTimeout \= 60
                    unit = TimeUnit.Minutes
                End If

                'How about hours?
                If cacheTimeout Mod 60 = 0 Then
                    cacheTimeout \= 60
                    unit = TimeUnit.Hours
                End If

                'Days?
                If cacheTimeout Mod 24 = 0 Then
                    cacheTimeout \= 24
                    unit = TimeUnit.Days
                End If
            End If

            Select Case unit
                Case TimeUnit.Seconds
                    TimeUnitComboBox.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Seconds
                Case TimeUnit.Minutes
                    TimeUnitComboBox.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Minutes
                Case TimeUnit.Hours
                    TimeUnitComboBox.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Hours
                Case TimeUnit.Days
                    TimeUnitComboBox.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_Days
            End Select

            TimeQuantity.Value = cacheTimeout
        End Sub

        Private Sub SetUseCustomConnectionStringControlValues(doc As XmlDocument)
            Dim connectionStringSpecified As Boolean
            Dim connectionString As String = ServicesPropPageAppConfigHelper.GetEffectiveDefaultConnectionString(doc, connectionStringSpecified, ProjectHierarchy)
            If Not connectionStringSpecified Then
                'There were connection strings, but they're not all the same connection string
                UseCustomConnectionStringCheckBox.Enabled = False
                UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Indeterminate
            ElseIf connectionString Is Nothing Then
                'The default value
                UseCustomConnectionStringCheckBox.Enabled = True
                UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Unchecked
                CustomConnectionString.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_connectionStringValueDefaultDisplayValue
                CustomConnectionString.Enabled = False
            Else
                'Using a non-default connection string for all providers
                UseCustomConnectionStringCheckBox.Enabled = True
                UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked
                CustomConnectionString.Text = connectionString
                CustomConnectionString.Enabled = True
            End If
            UpdateCustomConnectionStringControlBasedOnCheckState()
            Dim preferredHeight As Integer = CustomConnectionString.GetPreferredSize(New Drawing.Size(CustomConnectionString.Width, 0)).Height
            If CustomConnectionString.Height < preferredHeight Then CustomConnectionString.Height = preferredHeight
            SetDirtyFlag()
        End Sub

        Private Sub UseCustomConnectionStringCheckBox_CheckStateChanged(sender As Object, e As EventArgs) Handles UseCustomConnectionStringCheckBox.CheckStateChanged
            UpdateCustomConnectionStringControlBasedOnCheckState()
        End Sub

        Private Sub UpdateCustomConnectionStringControlBasedOnCheckState()
            Select Case UseCustomConnectionStringCheckBox.CheckState
                Case System.Windows.Forms.CheckState.Indeterminate
                    'The connection strings don't match
                    CustomConnectionString.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_ConnectionStringsDontMatch
                Case System.Windows.Forms.CheckState.Checked
                    'We're using a custom connection string
                    'Either the text has already been set (in which case we're good), or it's the display default message, in which case we should
                    'change it to the default value.
                    If CustomConnectionString.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_connectionStringValueDefaultDisplayValue Then
                        CustomConnectionString.Text = ServicesPropPageAppConfigHelper.ConnectionStringValueDefault
                    End If
                    ServicesPropPageAppConfigHelper.SetConnectionStringText(_appConfigDocument, CustomConnectionString.Text, ProjectHierarchy)
                Case System.Windows.Forms.CheckState.Unchecked
                    'We're using the default
                    CustomConnectionString.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_connectionStringValueDefaultDisplayValue
                    ServicesPropPageAppConfigHelper.SetConnectionStringText(_appConfigDocument, Nothing, ProjectHierarchy)
            End Select

            CustomConnectionString.Enabled = UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked

            SetDirtyFlag()
        End Sub

        Private Sub CustomConnectionString_TextChanged(sender As Object, e As EventArgs) Handles CustomConnectionString.TextChanged
            If CustomConnectionString.Enabled Then
                ServicesPropPageAppConfigHelper.SetConnectionStringText(_appConfigDocument, CustomConnectionString.Text, ProjectHierarchy)
                SetDirtyFlag()
            End If
        End Sub

        Private Sub TimeUnitComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TimeUnitComboBox.SelectedIndexChanged
            SetCacheTimeout()
        End Sub

        Private Sub TimeQuantity_ValueChanged(sender As Object, e As EventArgs) Handles TimeQuantity.ValueChanged
            SetCacheTimeout()
        End Sub

        Private Sub SetCacheTimeout()
            Dim seconds As Integer
            Dim multiplier As Integer

            Select Case TimeUnitComboBox.SelectedIndex
                Case 0 'Seconds
                    multiplier = 1
                Case 1 'Minutes
                    multiplier = 60
                Case 2 'Hours
                    multiplier = 60 * 60
                Case 3 'Days
                    multiplier = 60 * 60 * 24
                Case Else 'Setting for the first time, or something wacky happened
                    multiplier = 1
            End Select
            TimeQuantity.Maximum = Integer.MaxValue \ multiplier
            seconds = CInt(TimeQuantity.Value) * multiplier
            ServicesPropPageAppConfigHelper.SetCacheTimeout(_appConfigDocument, seconds, ProjectHierarchy)
            SetDirtyFlag()
        End Sub

        Private Sub SavePasswordHashLocallyCheckbox_CheckedChanged(sender As Object, e As EventArgs) Handles SavePasswordHashLocallyCheckbox.CheckedChanged
            ServicesPropPageAppConfigHelper.SetSavePasswordHashLocally(_appConfigDocument, SavePasswordHashLocallyCheckbox.Checked, ProjectHierarchy)
            SetDirtyFlag()
        End Sub

        Private Sub HonorServerCookieExpirySavePasswordHashLocallyCheckbox_CheckedChanged(sender As Object, e As EventArgs) Handles HonorServerCookieExpirationCheckbox.CheckedChanged
            ServicesPropPageAppConfigHelper.SetHonorCookieExpiry(_appConfigDocument, HonorServerCookieExpirationCheckbox.Checked, ProjectHierarchy)
            SetDirtyFlag()
        End Sub

        Private Sub SetDirtyFlag()
            IsDirty = _appConfigDocument IsNot Nothing AndAlso _appConfigDocument.OuterXml <> _savedXml
        End Sub
    End Class
End Namespace
