' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class ServicesPropPage
        Friend WithEvents TableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents HelpLabel As VSThemedLinkLabel
        Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AuthenticationProviderGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents AuthenticationServiceUrlLabel As System.Windows.Forms.Label
        Friend WithEvents WindowsBasedAuth As System.Windows.Forms.RadioButton
        Friend WithEvents FormBasedAuth As System.Windows.Forms.RadioButton
        Friend WithEvents CustomCredentialProviderType As System.Windows.Forms.TextBox
        Friend WithEvents CustomCredentialProviderTypeLabel As System.Windows.Forms.Label
        Friend WithEvents AuthenticationServiceUrl As System.Windows.Forms.TextBox
        Friend WithEvents RolesServiceUrlLabel As System.Windows.Forms.Label
        Friend WithEvents RolesServiceUrl As System.Windows.Forms.TextBox
        Friend WithEvents WebSettingsUrlLabel As System.Windows.Forms.Label
        Friend WithEvents WebSettingsUrl As System.Windows.Forms.TextBox
        Friend WithEvents AdvancedSettings As System.Windows.Forms.Button
        Friend WithEvents EnableApplicationServices As System.Windows.Forms.CheckBox

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ServicesPropPage))
            Me.EnableApplicationServices = New System.Windows.Forms.CheckBox
            Me.HelpLabel = New VSThemedLinkLabel
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            Me.AuthenticationProviderGroupBox = New System.Windows.Forms.GroupBox
            Me.TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel
            Me.WindowsBasedAuth = New System.Windows.Forms.RadioButton
            Me.CustomCredentialProviderType = New System.Windows.Forms.TextBox
            Me.FormBasedAuth = New System.Windows.Forms.RadioButton
            Me.CustomCredentialProviderTypeLabel = New System.Windows.Forms.Label
            Me.AuthenticationServiceUrlLabel = New System.Windows.Forms.Label
            Me.AuthenticationServiceUrl = New System.Windows.Forms.TextBox
            Me.RolesServiceUrlLabel = New System.Windows.Forms.Label
            Me.RolesServiceUrl = New System.Windows.Forms.TextBox
            Me.WebSettingsUrlLabel = New System.Windows.Forms.Label
            Me.WebSettingsUrl = New System.Windows.Forms.TextBox
            Me.AdvancedSettings = New System.Windows.Forms.Button
            Me.TableLayoutPanel1.SuspendLayout()
            Me.AuthenticationProviderGroupBox.SuspendLayout()
            Me.TableLayoutPanel2.SuspendLayout()
            Me.SuspendLayout()
            '
            'EnableApplicationServices
            '
            resources.ApplyResources(Me.EnableApplicationServices, "EnableApplicationServices")
            Me.TableLayoutPanel1.SetColumnSpan(Me.EnableApplicationServices, 2)
            Me.EnableApplicationServices.Name = "EnableApplicationServices"
            Me.EnableApplicationServices.UseVisualStyleBackColor = True
            '
            'HelpLabel
            '
            resources.ApplyResources(Me.HelpLabel, "HelpLabel")
            Me.TableLayoutPanel1.SetColumnSpan(Me.HelpLabel, 2)
            Me.HelpLabel.Name = "HelpLabel"
            Me.HelpLabel.TabStop = True
            Me.HelpLabel.SetThemedColor(VsUIShell5Service, SupportsTheming)
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(Me.TableLayoutPanel1, "TableLayoutPanel1")
            Me.TableLayoutPanel1.Controls.Add(Me.HelpLabel, 0, 0)
            Me.TableLayoutPanel1.Controls.Add(Me.EnableApplicationServices, 0, 1)
            Me.TableLayoutPanel1.Controls.Add(Me.AuthenticationProviderGroupBox, 1, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.RolesServiceUrlLabel, 1, 3)
            Me.TableLayoutPanel1.Controls.Add(Me.RolesServiceUrl, 1, 4)
            Me.TableLayoutPanel1.Controls.Add(Me.WebSettingsUrlLabel, 1, 5)
            Me.TableLayoutPanel1.Controls.Add(Me.WebSettingsUrl, 1, 6)
            Me.TableLayoutPanel1.Controls.Add(Me.AdvancedSettings, 1, 7)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
            '
            'AuthenticationProviderGroupBox
            '
            resources.ApplyResources(Me.AuthenticationProviderGroupBox, "AuthenticationProviderGroupBox")
            Me.AuthenticationProviderGroupBox.Controls.Add(Me.TableLayoutPanel2)
            Me.AuthenticationProviderGroupBox.Name = "AuthenticationProviderGroupBox"
            Me.AuthenticationProviderGroupBox.TabStop = False
            '
            'TableLayoutPanel2
            '
            resources.ApplyResources(Me.TableLayoutPanel2, "TableLayoutPanel2")
            Me.TableLayoutPanel2.Controls.Add(Me.WindowsBasedAuth, 0, 0)
            Me.TableLayoutPanel2.Controls.Add(Me.CustomCredentialProviderType, 0, 5)
            Me.TableLayoutPanel2.Controls.Add(Me.FormBasedAuth, 0, 1)
            Me.TableLayoutPanel2.Controls.Add(Me.CustomCredentialProviderTypeLabel, 0, 4)
            Me.TableLayoutPanel2.Controls.Add(Me.AuthenticationServiceUrlLabel, 0, 2)
            Me.TableLayoutPanel2.Controls.Add(Me.AuthenticationServiceUrl, 0, 3)
            Me.TableLayoutPanel2.Name = "TableLayoutPanel2"
            '
            'WindowsBasedAuth
            '
            resources.ApplyResources(Me.WindowsBasedAuth, "WindowsBasedAuth")
            Me.WindowsBasedAuth.Name = "WindowsBasedAuth"
            Me.WindowsBasedAuth.UseVisualStyleBackColor = True
            '
            'CustomCredentialProviderType
            '
            resources.ApplyResources(Me.CustomCredentialProviderType, "CustomCredentialProviderType")
            Me.CustomCredentialProviderType.Name = "CustomCredentialProviderType"
            '
            'FormBasedAuth
            '
            resources.ApplyResources(Me.FormBasedAuth, "FormBasedAuth")
            Me.FormBasedAuth.Checked = True
            Me.FormBasedAuth.Name = "FormBasedAuth"
            Me.FormBasedAuth.TabStop = True
            Me.FormBasedAuth.UseVisualStyleBackColor = True
            '
            'CustomCredentialProviderTypeLabel
            '
            resources.ApplyResources(Me.CustomCredentialProviderTypeLabel, "CustomCredentialProviderTypeLabel")
            Me.CustomCredentialProviderTypeLabel.Name = "CustomCredentialProviderTypeLabel"
            '
            'AuthenticationServiceUrlLabel
            '
            resources.ApplyResources(Me.AuthenticationServiceUrlLabel, "AuthenticationServiceUrlLabel")
            Me.AuthenticationServiceUrlLabel.Name = "AuthenticationServiceUrlLabel"
            '
            'AuthenticationServiceUrl
            '
            resources.ApplyResources(Me.AuthenticationServiceUrl, "AuthenticationServiceUrl")
            Me.AuthenticationServiceUrl.Name = "AuthenticationServiceUrl"
            '
            'RolesServiceUrlLabel
            '
            resources.ApplyResources(Me.RolesServiceUrlLabel, "RolesServiceUrlLabel")
            Me.RolesServiceUrlLabel.Name = "RolesServiceUrlLabel"
            '
            'RolesServiceUrl
            '
            resources.ApplyResources(Me.RolesServiceUrl, "RolesServiceUrl")
            Me.RolesServiceUrl.Name = "RolesServiceUrl"
            '
            'WebSettingsUrlLabel
            '
            resources.ApplyResources(Me.WebSettingsUrlLabel, "WebSettingsUrlLabel")
            Me.WebSettingsUrlLabel.Name = "WebSettingsUrlLabel"
            '
            'WebSettingsUrl
            '
            resources.ApplyResources(Me.WebSettingsUrl, "WebSettingsUrl")
            Me.WebSettingsUrl.Name = "WebSettingsUrl"
            '
            'AdvancedSettings
            '
            resources.ApplyResources(Me.AdvancedSettings, "AdvancedSettings")
            Me.AdvancedSettings.Name = "AdvancedSettings"
            Me.AdvancedSettings.UseVisualStyleBackColor = True
            '
            'ServicesPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.TableLayoutPanel1)
            Me.Name = "ServicesPropPage"
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.AuthenticationProviderGroupBox.ResumeLayout(False)
            Me.AuthenticationProviderGroupBox.PerformLayout()
            Me.TableLayoutPanel2.ResumeLayout(False)
            Me.TableLayoutPanel2.PerformLayout()
            Me.ResumeLayout(False)

        End Sub
    End Class

End Namespace