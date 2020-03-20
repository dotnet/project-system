' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF
    Partial Class ApplicationPropPageVBWPF

        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyNameLabel As System.Windows.Forms.Label
        Friend WithEvents RootNamespaceLabel As System.Windows.Forms.Label
        Friend WithEvents AssemblyNameTextBox As System.Windows.Forms.TextBox
        Friend WithEvents RootNamespaceTextBox As System.Windows.Forms.TextBox
        Friend WithEvents TargetFrameworkLabel As System.Windows.Forms.Label
        Friend WithEvents TargetFrameworkComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents AutoGenerateBindingRedirectsCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents ApplicationTypeLabel As System.Windows.Forms.Label
        Friend WithEvents ApplicationTypeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents AssemblyInfoButton As System.Windows.Forms.Button
        Friend WithEvents StartupObjectOrUriLabel As System.Windows.Forms.Label
        Friend WithEvents StartupObjectOrUriComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents UseApplicationFrameworkCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents IconLabel As System.Windows.Forms.Label
        Friend WithEvents TopHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents IconCombobox As System.Windows.Forms.ComboBox
        Friend WithEvents IconPicturebox As System.Windows.Forms.PictureBox
        Friend WithEvents WindowsAppGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents BottomHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents EditXamlButton As System.Windows.Forms.Button
        Friend WithEvents ViewCodeButton As System.Windows.Forms.Button
        Friend WithEvents ShutdownModeLabel As System.Windows.Forms.Label
        Friend WithEvents ShutdownModeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ButtonsLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ViewUACSettingsButton As System.Windows.Forms.Button
        Private _components As System.ComponentModel.IContainer

        <System.Diagnostics.DebuggerNonUserCode()>
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ApplicationPropPageVBWPF))
            Me.TopHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.ButtonsLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.AssemblyInfoButton = New System.Windows.Forms.Button
            Me.ViewUACSettingsButton = New System.Windows.Forms.Button
            Me.AssemblyNameLabel = New System.Windows.Forms.Label
            Me.AssemblyNameTextBox = New System.Windows.Forms.TextBox
            Me.RootNamespaceLabel = New System.Windows.Forms.Label
            Me.RootNamespaceTextBox = New System.Windows.Forms.TextBox
            Me.TargetFrameworkLabel = New System.Windows.Forms.Label
            Me.TargetFrameworkComboBox = New System.Windows.Forms.ComboBox
            Me.AutoGenerateBindingRedirectsCheckBox = New System.Windows.Forms.CheckBox
            Me.StartupObjectOrUriComboBox = New System.Windows.Forms.ComboBox
            Me.StartupObjectOrUriLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeComboBox = New System.Windows.Forms.ComboBox
            Me.UseApplicationFrameworkCheckBox = New System.Windows.Forms.CheckBox
            Me.IconLabel = New System.Windows.Forms.Label
            Me.IconPicturebox = New System.Windows.Forms.PictureBox
            Me.IconCombobox = New System.Windows.Forms.ComboBox
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.WindowsAppGroupBox = New System.Windows.Forms.GroupBox
            Me.BottomHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.EditXamlButton = New System.Windows.Forms.Button
            Me.ViewCodeButton = New System.Windows.Forms.Button
            Me.ShutdownModeLabel = New System.Windows.Forms.Label
            Me.ShutdownModeComboBox = New System.Windows.Forms.ComboBox
            Me.TopHalfLayoutPanel.SuspendLayout()
            Me.ButtonsLayoutPanel.SuspendLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.WindowsAppGroupBox.SuspendLayout()
            Me.BottomHalfLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TopHalfLayoutPanel
            '
            resources.ApplyResources(Me.TopHalfLayoutPanel, "TopHalfLayoutPanel")
            Me.TopHalfLayoutPanel.Controls.Add(Me.ButtonsLayoutPanel, 0, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameLabel, 0, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameTextBox, 0, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceLabel, 1, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceTextBox, 1, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkLabel, 0, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkComboBox, 0, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeLabel, 1, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeComboBox, 1, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AutoGenerateBindingRedirectsCheckBox, 0, 4)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectOrUriComboBox, 0, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectOrUriLabel, 0, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.UseApplicationFrameworkCheckBox, 0, 8)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconLabel, 1, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconPicturebox, 2, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconCombobox, 1, 6)
            Me.TopHalfLayoutPanel.Name = "TopHalfLayoutPanel"
            '
            'ButtonsLayoutPanel
            '
            resources.ApplyResources(Me.ButtonsLayoutPanel, "ButtonsLayoutPanel")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.ButtonsLayoutPanel, 2)
            Me.ButtonsLayoutPanel.Controls.Add(Me.AssemblyInfoButton, 0, 0)
            Me.ButtonsLayoutPanel.Controls.Add(Me.ViewUACSettingsButton, 1, 0)
            Me.ButtonsLayoutPanel.Name = "ButtonsLayoutPanel"
            '
            'AssemblyInfoButton
            '
            resources.ApplyResources(Me.AssemblyInfoButton, "AssemblyInfoButton")
            Me.AssemblyInfoButton.Name = "AssemblyInfoButton"
            '
            'ViewUACSettingsButton
            '
            resources.ApplyResources(Me.ViewUACSettingsButton, "ViewUACSettingsButton")
            Me.ViewUACSettingsButton.Name = "ViewUACSettingsButton"
            Me.ViewUACSettingsButton.UseVisualStyleBackColor = True
            '
            'AssemblyNameLabel
            '
            resources.ApplyResources(Me.AssemblyNameLabel, "AssemblyNameLabel")
            Me.AssemblyNameLabel.Name = "AssemblyNameLabel"
            '
            'AssemblyNameTextBox
            '
            resources.ApplyResources(Me.AssemblyNameTextBox, "AssemblyNameTextBox")
            Me.AssemblyNameTextBox.Name = "AssemblyNameTextBox"
            '
            'RootNamespaceLabel
            '
            resources.ApplyResources(Me.RootNamespaceLabel, "RootNamespaceLabel")
            Me.RootNamespaceLabel.Name = "RootNamespaceLabel"
            '
            'RootNamespaceTextBox
            '
            resources.ApplyResources(Me.RootNamespaceTextBox, "RootNamespaceTextBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.RootNamespaceTextBox, 2)
            Me.RootNamespaceTextBox.Name = "RootNamespaceTextBox"
            '
            'TargetFrameworkLabel
            '
            resources.ApplyResources(Me.TargetFrameworkLabel, "TargetFrameworkLabel")
            Me.TargetFrameworkLabel.Name = "TargetFrameworkLabel"
            '
            'TargetFrameworkComboBox
            '
            resources.ApplyResources(Me.TargetFrameworkComboBox, "TargetFrameworkComboBox")
            Me.TargetFrameworkComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TargetFrameworkComboBox.FormattingEnabled = True
            Me.TargetFrameworkComboBox.Name = "TargetFrameworkComboBox"
            '
            'AutoGenerateBindingRedirectsCheckBox
            '
            resources.ApplyResources(Me.AutoGenerateBindingRedirectsCheckBox, "AutoGenerateBindingRedirectsCheckBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.AutoGenerateBindingRedirectsCheckBox, 2)
            Me.AutoGenerateBindingRedirectsCheckBox.Name = "AutoGenerateBindingRedirectsCheckBox"
            '
            'StartupObjectOrUriComboBox
            '
            resources.ApplyResources(Me.StartupObjectOrUriComboBox, "StartupObjectOrUriComboBox")
            Me.StartupObjectOrUriComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.StartupObjectOrUriComboBox.FormattingEnabled = True
            Me.StartupObjectOrUriComboBox.Name = "StartupObjectOrUriComboBox"
            '
            'StartupObjectOrUriLabel
            '
            resources.ApplyResources(Me.StartupObjectOrUriLabel, "StartupObjectOrUriLabel")
            Me.StartupObjectOrUriLabel.Name = "StartupObjectOrUriLabel"
            '
            'ApplicationTypeLabel
            '
            resources.ApplyResources(Me.ApplicationTypeLabel, "ApplicationTypeLabel")
            Me.ApplicationTypeLabel.Name = "ApplicationTypeLabel"
            '
            'ApplicationTypeComboBox
            '
            resources.ApplyResources(Me.ApplicationTypeComboBox, "ApplicationTypeComboBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.ApplicationTypeComboBox, 2)
            Me.ApplicationTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ApplicationTypeComboBox.FormattingEnabled = True
            Me.ApplicationTypeComboBox.Name = "ApplicationTypeComboBox"
            '
            'UseApplicationFrameworkCheckBox
            '
            resources.ApplyResources(Me.UseApplicationFrameworkCheckBox, "UseApplicationFrameworkCheckBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.UseApplicationFrameworkCheckBox, 2)
            Me.UseApplicationFrameworkCheckBox.Name = "UseApplicationFrameworkCheckBox"
            '
            'IconLabel
            '
            resources.ApplyResources(Me.IconLabel, "IconLabel")
            Me.IconLabel.Name = "IconLabel"
            '
            'IconPicturebox
            '
            resources.ApplyResources(Me.IconPicturebox, "IconPicturebox")
            Me.IconPicturebox.Name = "IconPicturebox"
            Me.IconPicturebox.TabStop = False
            '
            'IconCombobox
            '
            resources.ApplyResources(Me.IconCombobox, "IconCombobox")
            Me.IconCombobox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
            Me.IconCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.IconCombobox.FormattingEnabled = True
            Me.IconCombobox.Name = "IconCombobox"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TopHalfLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.WindowsAppGroupBox, 0, 1)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'WindowsAppGroupBox
            '
            resources.ApplyResources(Me.WindowsAppGroupBox, "WindowsAppGroupBox")
            Me.WindowsAppGroupBox.Controls.Add(Me.BottomHalfLayoutPanel)
            Me.WindowsAppGroupBox.Name = "WindowsAppGroupBox"
            Me.WindowsAppGroupBox.TabStop = False
            '
            'BottomHalfLayoutPanel
            '
            resources.ApplyResources(Me.BottomHalfLayoutPanel, "BottomHalfLayoutPanel")
            Me.BottomHalfLayoutPanel.Controls.Add(Me.EditXamlButton, 0, 2)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ViewCodeButton, 1, 2)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeLabel, 0, 0)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeComboBox, 0, 1)
            Me.BottomHalfLayoutPanel.Name = "BottomHalfLayoutPanel"
            '
            'EditXamlButton
            '
            resources.ApplyResources(Me.EditXamlButton, "EditXamlButton")
            Me.EditXamlButton.Name = "EditXamlButton"
            '
            'ViewCodeButton
            '
            resources.ApplyResources(Me.ViewCodeButton, "ViewCodeButton")
            Me.ViewCodeButton.Name = "ViewCodeButton"
            '
            'ShutdownModeLabel
            '
            resources.ApplyResources(Me.ShutdownModeLabel, "ShutdownModeLabel")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.ShutdownModeLabel, 2)
            Me.ShutdownModeLabel.Name = "ShutdownModeLabel"
            '
            'ShutdownModeComboBox
            '
            resources.ApplyResources(Me.ShutdownModeComboBox, "ShutdownModeComboBox")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.ShutdownModeComboBox, 2)
            Me.ShutdownModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ShutdownModeComboBox.FormattingEnabled = True
            Me.ShutdownModeComboBox.Name = "ShutdownModeComboBox"
            '
            'ApplicationPropPageVBWPF
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "ApplicationPropPageVBWPF"
            Me.TopHalfLayoutPanel.ResumeLayout(False)
            Me.TopHalfLayoutPanel.PerformLayout()
            Me.ButtonsLayoutPanel.ResumeLayout(False)
            Me.ButtonsLayoutPanel.PerformLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).EndInit()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.WindowsAppGroupBox.ResumeLayout(False)
            Me.WindowsAppGroupBox.PerformLayout()
            Me.BottomHalfLayoutPanel.ResumeLayout(False)
            Me.BottomHalfLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
    End Class

End Namespace

