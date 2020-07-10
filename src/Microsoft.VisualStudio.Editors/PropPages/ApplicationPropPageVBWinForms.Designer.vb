Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class ApplicationPropPageVBWinForms

        Friend WithEvents SaveMySettingsCheckbox As System.Windows.Forms.CheckBox
        Friend WithEvents AuthenticationModeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents AuthenticationModeLabel As System.Windows.Forms.Label
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
        Friend WithEvents StartupObjectLabel As System.Windows.Forms.Label
        Friend WithEvents StartupObjectComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents UseApplicationFrameworkCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents IconLabel As System.Windows.Forms.Label
        Friend WithEvents EnableXPThemesCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents SingleInstanceCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents ShutdownModeLabel As System.Windows.Forms.Label
        Friend WithEvents ShutdownModeComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ViewCodeButton As System.Windows.Forms.Button
        Friend WithEvents TopHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents WindowsAppGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents BottomHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents IconCombobox As System.Windows.Forms.ComboBox
        Friend WithEvents IconPicturebox As System.Windows.Forms.PictureBox
        Friend WithEvents SplashScreenLabel As System.Windows.Forms.Label
        Friend WithEvents SplashScreenComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents ViewUACSettingsButton As System.Windows.Forms.Button
        Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel

        Private _components As System.ComponentModel.IContainer

        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ApplicationPropPageVBWinForms))
            Me.TopHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.AssemblyNameLabel = New System.Windows.Forms.Label
            Me.AssemblyNameTextBox = New System.Windows.Forms.TextBox
            Me.RootNamespaceLabel = New System.Windows.Forms.Label
            Me.RootNamespaceTextBox = New System.Windows.Forms.TextBox
            Me.TargetFrameworkLabel = New System.Windows.Forms.Label
            Me.TargetFrameworkComboBox = New System.Windows.Forms.ComboBox
            Me.AutoGenerateBindingRedirectsCheckBox = New System.Windows.Forms.CheckBox
            Me.StartupObjectComboBox = New System.Windows.Forms.ComboBox
            Me.StartupObjectLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeLabel = New System.Windows.Forms.Label
            Me.ApplicationTypeComboBox = New System.Windows.Forms.ComboBox
            Me.IconLabel = New System.Windows.Forms.Label
            Me.IconPicturebox = New System.Windows.Forms.PictureBox
            Me.IconCombobox = New System.Windows.Forms.ComboBox
            Me.UseApplicationFrameworkCheckBox = New System.Windows.Forms.CheckBox
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            Me.AssemblyInfoButton = New System.Windows.Forms.Button
            Me.ViewUACSettingsButton = New System.Windows.Forms.Button
            Me.WindowsAppGroupBox = New System.Windows.Forms.GroupBox
            Me.BottomHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.EnableXPThemesCheckBox = New System.Windows.Forms.CheckBox
            Me.SingleInstanceCheckBox = New System.Windows.Forms.CheckBox
            Me.SaveMySettingsCheckbox = New System.Windows.Forms.CheckBox
            Me.AuthenticationModeLabel = New System.Windows.Forms.Label
            Me.AuthenticationModeComboBox = New System.Windows.Forms.ComboBox
            Me.ShutdownModeLabel = New System.Windows.Forms.Label
            Me.ShutdownModeComboBox = New System.Windows.Forms.ComboBox
            Me.SplashScreenLabel = New System.Windows.Forms.Label
            Me.SplashScreenComboBox = New System.Windows.Forms.ComboBox
            Me.ViewCodeButton = New System.Windows.Forms.Button
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.TopHalfLayoutPanel.SuspendLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.TableLayoutPanel1.SuspendLayout()
            Me.WindowsAppGroupBox.SuspendLayout()
            Me.BottomHalfLayoutPanel.SuspendLayout()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TopHalfLayoutPanel
            '
            resources.ApplyResources(Me.TopHalfLayoutPanel, "TopHalfLayoutPanel")
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameLabel, 0, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameTextBox, 0, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceLabel, 1, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceTextBox, 1, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkLabel, 0, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkComboBox, 0, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeLabel, 1, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.ApplicationTypeComboBox, 1, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AutoGenerateBindingRedirectsCheckBox, 0, 4)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectComboBox, 0, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectLabel, 0, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconLabel, 1, 5)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconPicturebox, 2, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.IconCombobox, 1, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.UseApplicationFrameworkCheckBox, 0, 8)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TableLayoutPanel1, 0, 7)
            Me.TopHalfLayoutPanel.Name = "TopHalfLayoutPanel"
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
            'StartupObjectComboBox
            '
            resources.ApplyResources(Me.StartupObjectComboBox, "StartupObjectComboBox")
            Me.StartupObjectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.StartupObjectComboBox.FormattingEnabled = True
            Me.StartupObjectComboBox.Name = "StartupObjectComboBox"
            '
            'StartupObjectLabel
            '
            resources.ApplyResources(Me.StartupObjectLabel, "StartupObjectLabel")
            Me.StartupObjectLabel.Name = "StartupObjectLabel"
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
            'UseApplicationFrameworkCheckBox
            '
            resources.ApplyResources(Me.UseApplicationFrameworkCheckBox, "UseApplicationFrameworkCheckBox")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.UseApplicationFrameworkCheckBox, 2)
            Me.UseApplicationFrameworkCheckBox.Name = "UseApplicationFrameworkCheckBox"
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(Me.TableLayoutPanel1, "TableLayoutPanel1")
            Me.TopHalfLayoutPanel.SetColumnSpan(Me.TableLayoutPanel1, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.AssemblyInfoButton, 0, 0)
            Me.TableLayoutPanel1.Controls.Add(Me.ViewUACSettingsButton, 1, 0)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
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
            Me.BottomHalfLayoutPanel.Controls.Add(Me.EnableXPThemesCheckBox, 0, 0)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SingleInstanceCheckBox, 0, 1)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SaveMySettingsCheckbox, 0, 2)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.AuthenticationModeLabel, 0, 3)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.AuthenticationModeComboBox, 0, 4)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeLabel, 0, 5)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ShutdownModeComboBox, 0, 6)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SplashScreenLabel, 0, 7)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.SplashScreenComboBox, 0, 8)
            Me.BottomHalfLayoutPanel.Controls.Add(Me.ViewCodeButton, 1, 8)
            Me.BottomHalfLayoutPanel.Name = "BottomHalfLayoutPanel"
            '
            'EnableXPThemesCheckBox
            '
            resources.ApplyResources(Me.EnableXPThemesCheckBox, "EnableXPThemesCheckBox")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.EnableXPThemesCheckBox, 2)
            Me.EnableXPThemesCheckBox.Name = "EnableXPThemesCheckBox"
            '
            'SingleInstanceCheckBox
            '
            resources.ApplyResources(Me.SingleInstanceCheckBox, "SingleInstanceCheckBox")
            Me.BottomHalfLayoutPanel.SetColumnSpan(Me.SingleInstanceCheckBox, 2)
            Me.SingleInstanceCheckBox.Name = "SingleInstanceCheckBox"
            '
            'SaveMySettingsCheckbox
            '
            resources.ApplyResources(Me.SaveMySettingsCheckbox, "SaveMySettingsCheckbox")
            Me.SaveMySettingsCheckbox.Name = "SaveMySettingsCheckbox"
            '
            'AuthenticationModeLabel
            '
            resources.ApplyResources(Me.AuthenticationModeLabel, "AuthenticationModeLabel")
            Me.AuthenticationModeLabel.Name = "AuthenticationModeLabel"
            '
            'AuthenticationModeComboBox
            '
            resources.ApplyResources(Me.AuthenticationModeComboBox, "AuthenticationModeComboBox")
            Me.AuthenticationModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.AuthenticationModeComboBox.FormattingEnabled = True
            Me.AuthenticationModeComboBox.Name = "AuthenticationModeComboBox"
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
            Me.ShutdownModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.ShutdownModeComboBox.FormattingEnabled = True
            Me.ShutdownModeComboBox.Name = "ShutdownModeComboBox"
            '
            'SplashScreenLabel
            '
            resources.ApplyResources(Me.SplashScreenLabel, "SplashScreenLabel")
            Me.SplashScreenLabel.Name = "SplashScreenLabel"
            '
            'SplashScreenComboBox
            '
            resources.ApplyResources(Me.SplashScreenComboBox, "SplashScreenComboBox")
            Me.SplashScreenComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.SplashScreenComboBox.FormattingEnabled = True
            Me.SplashScreenComboBox.Name = "SplashScreenComboBox"
            '
            'ViewCodeButton
            '
            resources.ApplyResources(Me.ViewCodeButton, "ViewCodeButton")
            Me.ViewCodeButton.Name = "ViewCodeButton"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TopHalfLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.WindowsAppGroupBox, 0, 1)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'ApplicationPropPageVBWinForms
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "ApplicationPropPageVBWinForms"
            Me.TopHalfLayoutPanel.ResumeLayout(False)
            Me.TopHalfLayoutPanel.PerformLayout()
            CType(Me.IconPicturebox, System.ComponentModel.ISupportInitialize).EndInit()
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.WindowsAppGroupBox.ResumeLayout(False)
            Me.WindowsAppGroupBox.PerformLayout()
            Me.BottomHalfLayoutPanel.ResumeLayout(False)
            Me.BottomHalfLayoutPanel.PerformLayout()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

    End Class

End Namespace

