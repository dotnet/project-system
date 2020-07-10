
Namespace Microsoft.VisualStudio.Editors.PropertyPages

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class ApplicationPropPage

        Private components As System.ComponentModel.IContainer
        Friend WithEvents AssemblyName As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyInfoButton As System.Windows.Forms.Button
        Friend WithEvents OutputType As System.Windows.Forms.ComboBox
        Friend WithEvents StartupObject As System.Windows.Forms.ComboBox
        Friend WithEvents ApplicationIconLabel As System.Windows.Forms.Label
        Friend WithEvents ApplicationIcon As System.Windows.Forms.ComboBox
        Friend WithEvents AppIconBrowse As System.Windows.Forms.Button
        Friend WithEvents AppIconImage As System.Windows.Forms.PictureBox
        Friend WithEvents ApplicationManifestLabel As System.Windows.Forms.Label
        Friend WithEvents ApplicationManifest As System.Windows.Forms.ComboBox
        Friend WithEvents RootNamespaceTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyNameLabel As System.Windows.Forms.Label
        Friend WithEvents RootNamespaceLabel As System.Windows.Forms.Label
        Friend WithEvents OutputTypeLabel As System.Windows.Forms.Label
        Friend WithEvents ResourcesLabel As System.Windows.Forms.Label
        Friend WithEvents StartupObjectLabel As System.Windows.Forms.Label
        Friend WithEvents ResourcesGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents IconRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents Win32ResourceRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents Win32ResourceFileBrowse As System.Windows.Forms.Button
        Friend WithEvents Win32ResourceFile As System.Windows.Forms.TextBox
        Friend WithEvents TopHalfLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents TargetFramework As System.Windows.Forms.ComboBox
        Friend WithEvents TargetFrameworkLabel As System.Windows.Forms.Label
        Friend WithEvents AutoGenerateBindingRedirects As System.Windows.Forms.CheckBox
        Friend WithEvents overarchingLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ManifestExplanationLabel As System.Windows.Forms.Label
        Friend WithEvents iconTableLayoutPanel As System.Windows.Forms.TableLayoutPanel

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ApplicationPropPage))
            Me.TopHalfLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.AssemblyNameLabel = New System.Windows.Forms.Label()
            Me.AssemblyName = New System.Windows.Forms.TextBox()
            Me.RootNamespaceLabel = New System.Windows.Forms.Label()
            Me.RootNamespaceTextBox = New System.Windows.Forms.TextBox()
            Me.OutputTypeLabel = New System.Windows.Forms.Label()
            Me.OutputType = New System.Windows.Forms.ComboBox()
            Me.StartupObjectLabel = New System.Windows.Forms.Label()
            Me.StartupObject = New System.Windows.Forms.ComboBox()
            Me.AssemblyInfoButton = New System.Windows.Forms.Button()
            Me.TargetFrameworkLabel = New System.Windows.Forms.Label()
            Me.TargetFramework = New System.Windows.Forms.ComboBox()
            Me.AutoGenerateBindingRedirects = New System.Windows.Forms.CheckBox()
            Me.ResourcesGroupBox = New System.Windows.Forms.GroupBox()
            Me.iconTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.ResourcesLabel = New System.Windows.Forms.Label()
            Me.IconRadioButton = New System.Windows.Forms.RadioButton()
            Me.ManifestExplanationLabel = New System.Windows.Forms.Label()
            Me.ApplicationIconLabel = New System.Windows.Forms.Label()
            Me.ApplicationIcon = New System.Windows.Forms.ComboBox()
            Me.AppIconBrowse = New System.Windows.Forms.Button()
            Me.AppIconImage = New System.Windows.Forms.PictureBox()
            Me.ApplicationManifestLabel = New System.Windows.Forms.Label()
            Me.ApplicationManifest = New System.Windows.Forms.ComboBox()
            Me.Win32ResourceRadioButton = New System.Windows.Forms.RadioButton()
            Me.Win32ResourceFile = New System.Windows.Forms.TextBox()
            Me.Win32ResourceFileBrowse = New System.Windows.Forms.Button()
            Me.overarchingLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.TopHalfLayoutPanel.SuspendLayout()
            Me.ResourcesGroupBox.SuspendLayout()
            Me.iconTableLayoutPanel.SuspendLayout()
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.overarchingLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TopHalfLayoutPanel
            '
            resources.ApplyResources(Me.TopHalfLayoutPanel, "TopHalfLayoutPanel")
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyNameLabel, 0, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyName, 0, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceLabel, 1, 0)
            Me.TopHalfLayoutPanel.Controls.Add(Me.RootNamespaceTextBox, 1, 1)
            Me.TopHalfLayoutPanel.Controls.Add(Me.OutputTypeLabel, 1, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.OutputType, 1, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObjectLabel, 0, 6)
            Me.TopHalfLayoutPanel.Controls.Add(Me.StartupObject, 0, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AssemblyInfoButton, 1, 7)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFrameworkLabel, 0, 2)
            Me.TopHalfLayoutPanel.Controls.Add(Me.TargetFramework, 0, 3)
            Me.TopHalfLayoutPanel.Controls.Add(Me.AutoGenerateBindingRedirects, 0, 4)
            Me.TopHalfLayoutPanel.Name = "TopHalfLayoutPanel"
            '
            'AssemblyNameLabel
            '
            resources.ApplyResources(Me.AssemblyNameLabel, "AssemblyNameLabel")
            Me.AssemblyNameLabel.Name = "AssemblyNameLabel"
            '
            'AssemblyName
            '
            resources.ApplyResources(Me.AssemblyName, "AssemblyName")
            Me.AssemblyName.Name = "AssemblyName"
            '
            'RootNamespaceLabel
            '
            resources.ApplyResources(Me.RootNamespaceLabel, "RootNamespaceLabel")
            Me.RootNamespaceLabel.Name = "RootNamespaceLabel"
            '
            'RootNameSpace
            '
            resources.ApplyResources(Me.RootNamespaceTextBox, "RootNameSpace")
            Me.RootNamespaceTextBox.Name = "RootNameSpace"
            '
            'OutputTypeLabel
            '
            resources.ApplyResources(Me.OutputTypeLabel, "OutputTypeLabel")
            Me.OutputTypeLabel.Name = "OutputTypeLabel"
            '
            'OutputType
            '
            resources.ApplyResources(Me.OutputType, "OutputType")
            Me.OutputType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OutputType.FormattingEnabled = True
            Me.OutputType.Name = "OutputType"
            '
            'StartupObjectLabel
            '
            resources.ApplyResources(Me.StartupObjectLabel, "StartupObjectLabel")
            Me.StartupObjectLabel.Name = "StartupObjectLabel"
            '
            'StartupObject
            '
            resources.ApplyResources(Me.StartupObject, "StartupObject")
            Me.StartupObject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.StartupObject.FormattingEnabled = True
            Me.StartupObject.Name = "StartupObject"
            Me.StartupObject.Sorted = True
            '
            'AssemblyInfoButton
            '
            resources.ApplyResources(Me.AssemblyInfoButton, "AssemblyInfoButton")
            Me.AssemblyInfoButton.Name = "AssemblyInfoButton"
            '
            'TargetFrameworkLabel
            '
            resources.ApplyResources(Me.TargetFrameworkLabel, "TargetFrameworkLabel")
            Me.TargetFrameworkLabel.Name = "TargetFrameworkLabel"
            '
            'TargetFramework
            '
            resources.ApplyResources(Me.TargetFramework, "TargetFramework")
            Me.TargetFramework.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TargetFramework.FormattingEnabled = True
            Me.TargetFramework.Name = "TargetFramework"
            Me.TargetFramework.Sorted = True
            '
            'AutoGenerateBindingRedirects
            resources.ApplyResources(Me.AutoGenerateBindingRedirects, "AutoGenerateBindingRedirects")
            Me.AutoGenerateBindingRedirects.Name = "AutoGenerateBindingRedirects"
            '
            '
            'ResourcesGroupBox
            '
            resources.ApplyResources(Me.ResourcesGroupBox, "ResourcesGroupBox")
            Me.ResourcesGroupBox.Controls.Add(Me.iconTableLayoutPanel)
            Me.ResourcesGroupBox.Name = "ResourcesGroupBox"
            Me.ResourcesGroupBox.TabStop = False
            '
            'iconTableLayoutPanel
            '
            resources.ApplyResources(Me.iconTableLayoutPanel, "iconTableLayoutPanel")
            Me.iconTableLayoutPanel.Controls.Add(Me.ResourcesLabel, 0, 0)
            Me.iconTableLayoutPanel.Controls.Add(Me.IconRadioButton, 0, 1)
            Me.iconTableLayoutPanel.Controls.Add(Me.ManifestExplanationLabel, 0, 2)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationIconLabel, 0, 3)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationIcon, 0, 4)
            Me.iconTableLayoutPanel.Controls.Add(Me.AppIconBrowse, 1, 4)
            Me.iconTableLayoutPanel.Controls.Add(Me.AppIconImage, 2, 4)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationManifestLabel, 0, 5)
            Me.iconTableLayoutPanel.Controls.Add(Me.ApplicationManifest, 0, 6)
            Me.iconTableLayoutPanel.Controls.Add(Me.Win32ResourceRadioButton, 0, 7)
            Me.iconTableLayoutPanel.Controls.Add(Me.Win32ResourceFile, 0, 8)
            Me.iconTableLayoutPanel.Controls.Add(Me.Win32ResourceFileBrowse, 1, 8)
            Me.iconTableLayoutPanel.Name = "iconTableLayoutPanel"
            '
            'ResourcesLabel
            '
            resources.ApplyResources(Me.ResourcesLabel, "ResourcesLabel")
            Me.ResourcesLabel.Name = "ResourcesLabel"
            '
            'IconRadioButton
            '
            resources.ApplyResources(Me.IconRadioButton, "IconRadioButton")
            Me.IconRadioButton.Name = "IconRadioButton"
            '
            'ManifestExplanationLabel
            '
            Me.ManifestExplanationLabel.BorderStyle = System.Windows.Forms.BorderStyle.None
            resources.ApplyResources(Me.ManifestExplanationLabel, "ManifestExplanationLabel")
            Me.ManifestExplanationLabel.Name = "ManifestExplanationLabel"
            '
            'ApplicationIconLabel
            '
            resources.ApplyResources(Me.ApplicationIconLabel, "ApplicationIconLabel")
            Me.ApplicationIconLabel.Name = "ApplicationIconLabel"
            '
            'ApplicationIcon
            '
            resources.ApplyResources(Me.ApplicationIcon, "ApplicationIcon")
            Me.ApplicationIcon.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.ApplicationIcon.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem
            Me.ApplicationIcon.FormattingEnabled = True
            Me.ApplicationIcon.Name = "ApplicationIcon"
            '
            'AppIconBrowse
            '
            resources.ApplyResources(Me.AppIconBrowse, "AppIconBrowse")
            Me.AppIconBrowse.Name = "AppIconBrowse"
            '
            'AppIconImage
            '
            resources.ApplyResources(Me.AppIconImage, "AppIconImage")
            Me.AppIconImage.Name = "AppIconImage"
            Me.AppIconImage.TabStop = False
            '
            'ApplicationManifestLabel
            '
            resources.ApplyResources(Me.ApplicationManifestLabel, "ApplicationManifestLabel")
            Me.ApplicationManifestLabel.Name = "ApplicationManifestLabel"
            '
            'ApplicationManifest
            '
            resources.ApplyResources(Me.ApplicationManifest, "ApplicationManifest")
            Me.ApplicationManifest.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.ApplicationManifest.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem
            Me.ApplicationManifest.FormattingEnabled = True
            Me.ApplicationManifest.Name = "ApplicationManifest"
            '
            'Win32ResourceRadioButton
            '
            resources.ApplyResources(Me.Win32ResourceRadioButton, "Win32ResourceRadioButton")
            Me.Win32ResourceRadioButton.Name = "Win32ResourceRadioButton"
            '
            'Win32ResourceFile
            '
            resources.ApplyResources(Me.Win32ResourceFile, "Win32ResourceFile")
            Me.Win32ResourceFile.Name = "Win32ResourceFile"
            '
            'Win32ResourceFileBrowse
            '
            resources.ApplyResources(Me.Win32ResourceFileBrowse, "Win32ResourceFileBrowse")
            Me.Win32ResourceFileBrowse.Name = "Win32ResourceFileBrowse"
            '
            'overarchingLayoutPanel
            '
            resources.ApplyResources(Me.overarchingLayoutPanel, "overarchingLayoutPanel")
            Me.overarchingLayoutPanel.Controls.Add(Me.ResourcesGroupBox, 0, 1)
            Me.overarchingLayoutPanel.Controls.Add(Me.TopHalfLayoutPanel, 0, 0)
            Me.overarchingLayoutPanel.Name = "overarchingLayoutPanel"
            '
            'ApplicationPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingLayoutPanel)
            Me.Name = "ApplicationPropPage"
            Me.TopHalfLayoutPanel.ResumeLayout(False)
            Me.TopHalfLayoutPanel.PerformLayout()
            Me.ResourcesGroupBox.ResumeLayout(False)
            Me.ResourcesGroupBox.PerformLayout()
            Me.iconTableLayoutPanel.ResumeLayout(False)
            Me.iconTableLayoutPanel.PerformLayout()
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).EndInit()
            Me.overarchingLayoutPanel.ResumeLayout(False)
            Me.overarchingLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub
    End Class

End Namespace
