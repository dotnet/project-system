' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class PackagePropPage

        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PackagePropPage))
            Me.TableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.GeneratePackageOnBuild = New System.Windows.Forms.CheckBox()
            Me.PackageRequireLicenseAcceptance = New System.Windows.Forms.CheckBox()
            Me.PackageIdLabel = New System.Windows.Forms.Label()
            Me.PackageId = New System.Windows.Forms.TextBox()
            Me.PackageVersionLabel = New System.Windows.Forms.Label()
            Me.PackageVersion = New System.Windows.Forms.TextBox()
            Me.AuthorsLabel = New System.Windows.Forms.Label()
            Me.Authors = New System.Windows.Forms.TextBox()
            Me.AssemblyCompanyLabel = New System.Windows.Forms.Label()
            Me.AssemblyCompany = New System.Windows.Forms.TextBox()
            Me.ProductLabel = New System.Windows.Forms.Label()
            Me.Product = New System.Windows.Forms.TextBox()
            Me.DescriptionLabel = New System.Windows.Forms.Label()
            Me.Description = New System.Windows.Forms.TextBox()
            Me.CopyrightLabel = New System.Windows.Forms.Label()
            Me.Copyright = New System.Windows.Forms.TextBox()
            Me.PackageLicenseLabel = New System.Windows.Forms.Label()
            Me.LicenseLineLabel = New System.Windows.Forms.Label()
            Me.LicenseUrlWarning = New Microsoft.VisualStudio.Editors.PropertyPages.FixedWidthTextBox()
            Me.ExpressionLabel = New System.Windows.Forms.Label()
            Me.ExpressionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.LicenseExpressionRadioButton = New System.Windows.Forms.RadioButton()
            Me.PackageLicenseExpression = New System.Windows.Forms.TextBox()
            Me.FileLabel = New System.Windows.Forms.Label()
            Me.LicenseFileLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.LicenseFileRadioButton = New System.Windows.Forms.RadioButton()
            Me.LicenseFileNameTextBox = New System.Windows.Forms.TextBox()
            Me.LicenseBrowseButton = New System.Windows.Forms.Button()
            Me.PackageProjectUrlLabel = New System.Windows.Forms.Label()
            Me.PackageProjectUrl = New System.Windows.Forms.TextBox()
            Me.PackageIconLabel = New System.Windows.Forms.Label()
            Me.IconFileLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.IconFileBrowseButton = New System.Windows.Forms.Button()
            Me.PackageIcon = New System.Windows.Forms.TextBox()
            Me.RepositoryUrlLabel = New System.Windows.Forms.Label()
            Me.RepositoryUrl = New System.Windows.Forms.TextBox()
            Me.RepositoryTypeLabel = New System.Windows.Forms.Label()
            Me.RepositoryType = New System.Windows.Forms.TextBox()
            Me.PackageTagsLabel = New System.Windows.Forms.Label()
            Me.PackageTags = New System.Windows.Forms.TextBox()
            Me.PackageReleaseNotesLabel = New System.Windows.Forms.Label()
            Me.PackageReleaseNotes = New System.Windows.Forms.TextBox()
            Me.NeutralLanguageLabel = New System.Windows.Forms.Label()
            Me.NeutralLanguageComboBox = New System.Windows.Forms.ComboBox()
            Me.AssemblyVersionLabel = New System.Windows.Forms.Label()
            Me.AssemblyVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.AssemblyVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyFileVersionLabel = New System.Windows.Forms.Label()
            Me.FileVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.FileVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.PackageIconLineLabel = New System.Windows.Forms.Label()
            Me.PackageIconUrlWarning = New Microsoft.VisualStudio.Editors.PropertyPages.FixedWidthTextBox()
            Me.LicenseLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.OrLabel = New System.Windows.Forms.Label()
            Me.TableLayoutPanel.SuspendLayout()
            Me.ExpressionLayoutPanel.SuspendLayout()
            Me.LicenseFileLayoutPanel.SuspendLayout()
            Me.IconFileLayoutPanel.SuspendLayout()
            Me.AssemblyVersionLayoutPanel.SuspendLayout()
            Me.FileVersionLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TableLayoutPanel
            '
            resources.ApplyResources(Me.TableLayoutPanel, "TableLayoutPanel")
            Me.TableLayoutPanel.Controls.Add(Me.GeneratePackageOnBuild, 0, 0)
            Me.TableLayoutPanel.Controls.Add(Me.PackageRequireLicenseAcceptance, 0, 1)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIdLabel, 0, 2)
            Me.TableLayoutPanel.Controls.Add(Me.PackageId, 1, 2)
            Me.TableLayoutPanel.Controls.Add(Me.PackageVersionLabel, 0, 3)
            Me.TableLayoutPanel.Controls.Add(Me.PackageVersion, 1, 3)
            Me.TableLayoutPanel.Controls.Add(Me.AuthorsLabel, 0, 4)
            Me.TableLayoutPanel.Controls.Add(Me.Authors, 1, 4)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyCompanyLabel, 0, 5)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyCompany, 1, 5)
            Me.TableLayoutPanel.Controls.Add(Me.ProductLabel, 0, 6)
            Me.TableLayoutPanel.Controls.Add(Me.Product, 1, 6)
            Me.TableLayoutPanel.Controls.Add(Me.DescriptionLabel, 0, 7)
            Me.TableLayoutPanel.Controls.Add(Me.Description, 1, 7)
            Me.TableLayoutPanel.Controls.Add(Me.CopyrightLabel, 0, 8)
            Me.TableLayoutPanel.Controls.Add(Me.Copyright, 1, 8)
            Me.TableLayoutPanel.Controls.Add(Me.PackageLicenseLabel, 0, 9)
            Me.TableLayoutPanel.Controls.Add(Me.LicenseLineLabel, 1, 9)
            Me.TableLayoutPanel.Controls.Add(Me.LicenseUrlWarning, 2, 9)
            Me.TableLayoutPanel.Controls.Add(Me.ExpressionLabel, 0, 10)
            Me.TableLayoutPanel.Controls.Add(Me.ExpressionLayoutPanel, 1, 10)
            Me.TableLayoutPanel.Controls.Add(Me.FileLabel, 0, 11)
            Me.TableLayoutPanel.Controls.Add(Me.LicenseFileLayoutPanel, 1, 11)
            Me.TableLayoutPanel.Controls.Add(Me.PackageProjectUrlLabel, 0, 12)
            Me.TableLayoutPanel.Controls.Add(Me.PackageProjectUrl, 1, 12)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIconLabel, 0, 14)
            Me.TableLayoutPanel.Controls.Add(Me.IconFileLayoutPanel, 1, 14)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryUrlLabel, 0, 15)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryUrl, 1, 15)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryTypeLabel, 0, 16)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryType, 1, 16)
            Me.TableLayoutPanel.Controls.Add(Me.PackageTagsLabel, 0, 17)
            Me.TableLayoutPanel.Controls.Add(Me.PackageTags, 1, 17)
            Me.TableLayoutPanel.Controls.Add(Me.PackageReleaseNotesLabel, 0, 18)
            Me.TableLayoutPanel.Controls.Add(Me.PackageReleaseNotes, 1, 18)
            Me.TableLayoutPanel.Controls.Add(Me.NeutralLanguageLabel, 0, 19)
            Me.TableLayoutPanel.Controls.Add(Me.NeutralLanguageComboBox, 1, 19)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyVersionLabel, 0, 20)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyVersionLayoutPanel, 1, 20)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyFileVersionLabel, 0, 21)
            Me.TableLayoutPanel.Controls.Add(Me.FileVersionLayoutPanel, 1, 21)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIconLineLabel, 1, 13)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIconUrlWarning, 2, 13)
            Me.TableLayoutPanel.Name = "TableLayoutPanel"
            '
            'GeneratePackageOnBuild
            '
            resources.ApplyResources(Me.GeneratePackageOnBuild, "GeneratePackageOnBuild")
            Me.GeneratePackageOnBuild.Name = "GeneratePackageOnBuild"
            Me.GeneratePackageOnBuild.UseVisualStyleBackColor = True
            '
            'PackageRequireLicenseAcceptance
            '
            resources.ApplyResources(Me.PackageRequireLicenseAcceptance, "PackageRequireLicenseAcceptance")
            Me.TableLayoutPanel.SetColumnSpan(Me.PackageRequireLicenseAcceptance, 2)
            Me.PackageRequireLicenseAcceptance.Name = "PackageRequireLicenseAcceptance"
            Me.PackageRequireLicenseAcceptance.UseVisualStyleBackColor = True
            '
            'PackageIdLabel
            '
            resources.ApplyResources(Me.PackageIdLabel, "PackageIdLabel")
            Me.PackageIdLabel.Name = "PackageIdLabel"
            '
            'PackageId
            '
            resources.ApplyResources(Me.PackageId, "PackageId")
            Me.PackageId.Name = "PackageId"
            '
            'PackageVersionLabel
            '
            resources.ApplyResources(Me.PackageVersionLabel, "PackageVersionLabel")
            Me.PackageVersionLabel.Name = "PackageVersionLabel"
            '
            'PackageVersion
            '
            resources.ApplyResources(Me.PackageVersion, "PackageVersion")
            Me.PackageVersion.Name = "PackageVersion"
            '
            'AuthorsLabel
            '
            resources.ApplyResources(Me.AuthorsLabel, "AuthorsLabel")
            Me.AuthorsLabel.Name = "AuthorsLabel"
            '
            'Authors
            '
            resources.ApplyResources(Me.Authors, "Authors")
            Me.Authors.Name = "Authors"
            '
            'AssemblyCompanyLabel
            '
            resources.ApplyResources(Me.AssemblyCompanyLabel, "AssemblyCompanyLabel")
            Me.AssemblyCompanyLabel.Name = "AssemblyCompanyLabel"
            '
            'AssemblyCompany
            '
            resources.ApplyResources(Me.AssemblyCompany, "AssemblyCompany")
            Me.AssemblyCompany.Name = "AssemblyCompany"
            '
            'ProductLabel
            '
            resources.ApplyResources(Me.ProductLabel, "ProductLabel")
            Me.ProductLabel.Name = "ProductLabel"
            '
            'Product
            '
            resources.ApplyResources(Me.Product, "Product")
            Me.Product.Name = "Product"
            '
            'DescriptionLabel
            '
            resources.ApplyResources(Me.DescriptionLabel, "DescriptionLabel")
            Me.DescriptionLabel.Name = "DescriptionLabel"
            '
            'Description
            '
            resources.ApplyResources(Me.Description, "Description")
            Me.Description.Name = "Description"
            '
            'CopyrightLabel
            '
            resources.ApplyResources(Me.CopyrightLabel, "CopyrightLabel")
            Me.CopyrightLabel.Name = "CopyrightLabel"
            '
            'Copyright
            '
            resources.ApplyResources(Me.Copyright, "Copyright")
            Me.Copyright.Name = "Copyright"
            '
            'PackageLicenseLabel
            '
            resources.ApplyResources(Me.PackageLicenseLabel, "PackageLicenseLabel")
            Me.PackageLicenseLabel.Name = "PackageLicenseLabel"
            '
            'LicenseLineLabel
            '
            Me.LicenseLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.LicenseLineLabel, "LicenseLineLabel")
            Me.LicenseLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.LicenseLineLabel.Name = "LicenseLineLabel"
            '
            'LicenseUrlWarning
            '
            Me.LicenseUrlWarning.BorderStyle = System.Windows.Forms.BorderStyle.None
            resources.ApplyResources(Me.LicenseUrlWarning, "LicenseUrlWarning")
            Me.LicenseUrlWarning.Name = "LicenseUrlWarning"
            Me.LicenseUrlWarning.ReadOnly = True
            '
            'ExpressionLabel
            '
            resources.ApplyResources(Me.ExpressionLabel, "ExpressionLabel")
            Me.ExpressionLabel.Name = "ExpressionLabel"
            '
            'ExpressionLayoutPanel
            '
            Me.ExpressionLayoutPanel.CausesValidation = False
            resources.ApplyResources(Me.ExpressionLayoutPanel, "ExpressionLayoutPanel")
            Me.ExpressionLayoutPanel.Controls.Add(Me.LicenseExpressionRadioButton, 0, 0)
            Me.ExpressionLayoutPanel.Controls.Add(Me.PackageLicenseExpression, 1, 0)
            Me.ExpressionLayoutPanel.Name = "ExpressionLayoutPanel"
            '
            'LicenseExpressionRadioButton
            '
            resources.ApplyResources(Me.LicenseExpressionRadioButton, "LicenseExpressionRadioButton")
            Me.LicenseExpressionRadioButton.Name = "LicenseExpressionRadioButton"
            '
            'PackageLicenseExpression
            '
            resources.ApplyResources(Me.PackageLicenseExpression, "PackageLicenseExpression")
            Me.PackageLicenseExpression.Name = "PackageLicenseExpression"
            '
            'FileLabel
            '
            resources.ApplyResources(Me.FileLabel, "FileLabel")
            Me.FileLabel.Name = "FileLabel"
            '
            'LicenseFileLayoutPanel
            '
            resources.ApplyResources(Me.LicenseFileLayoutPanel, "LicenseFileLayoutPanel")
            Me.LicenseFileLayoutPanel.Controls.Add(Me.LicenseFileRadioButton, 0, 0)
            Me.LicenseFileLayoutPanel.Controls.Add(Me.LicenseFileNameTextBox, 1, 0)
            Me.LicenseFileLayoutPanel.Controls.Add(Me.LicenseBrowseButton, 2, 0)
            Me.LicenseFileLayoutPanel.Name = "LicenseFileLayoutPanel"
            '
            'LicenseFileRadioButton
            '
            resources.ApplyResources(Me.LicenseFileRadioButton, "LicenseFileRadioButton")
            Me.LicenseFileRadioButton.Name = "LicenseFileRadioButton"
            '
            'LicenseFileNameTextBox
            '
            resources.ApplyResources(Me.LicenseFileNameTextBox, "LicenseFileNameTextBox")
            Me.LicenseFileNameTextBox.Name = "LicenseFileNameTextBox"
            '
            'LicenseBrowseButton
            '
            resources.ApplyResources(Me.LicenseBrowseButton, "LicenseBrowseButton")
            Me.LicenseBrowseButton.Name = "LicenseBrowseButton"
            '
            'PackageProjectUrlLabel
            '
            resources.ApplyResources(Me.PackageProjectUrlLabel, "PackageProjectUrlLabel")
            Me.PackageProjectUrlLabel.Name = "PackageProjectUrlLabel"
            '
            'PackageProjectUrl
            '
            resources.ApplyResources(Me.PackageProjectUrl, "PackageProjectUrl")
            Me.PackageProjectUrl.Name = "PackageProjectUrl"
            '
            'PackageIconLabel
            '
            resources.ApplyResources(Me.PackageIconLabel, "PackageIconLabel")
            Me.PackageIconLabel.Name = "PackageIconLabel"
            '
            'IconFileLayoutPanel
            '
            resources.ApplyResources(Me.IconFileLayoutPanel, "IconFileLayoutPanel")
            Me.IconFileLayoutPanel.Controls.Add(Me.IconFileBrowseButton, 1, 0)
            Me.IconFileLayoutPanel.Controls.Add(Me.PackageIcon, 0, 0)
            Me.IconFileLayoutPanel.Name = "IconFileLayoutPanel"
            '
            'IconFileBrowseButton
            '
            resources.ApplyResources(Me.IconFileBrowseButton, "IconFileBrowseButton")
            Me.IconFileBrowseButton.Name = "IconFileBrowseButton"
            '
            'PackageIcon
            '
            resources.ApplyResources(Me.PackageIcon, "PackageIcon")
            Me.PackageIcon.Name = "PackageIcon"
            '
            'RepositoryUrlLabel
            '
            resources.ApplyResources(Me.RepositoryUrlLabel, "RepositoryUrlLabel")
            Me.RepositoryUrlLabel.Name = "RepositoryUrlLabel"
            '
            'RepositoryUrl
            '
            resources.ApplyResources(Me.RepositoryUrl, "RepositoryUrl")
            Me.RepositoryUrl.Name = "RepositoryUrl"
            '
            'RepositoryTypeLabel
            '
            resources.ApplyResources(Me.RepositoryTypeLabel, "RepositoryTypeLabel")
            Me.RepositoryTypeLabel.Name = "RepositoryTypeLabel"
            '
            'RepositoryType
            '
            resources.ApplyResources(Me.RepositoryType, "RepositoryType")
            Me.RepositoryType.Name = "RepositoryType"
            '
            'PackageTagsLabel
            '
            resources.ApplyResources(Me.PackageTagsLabel, "PackageTagsLabel")
            Me.PackageTagsLabel.Name = "PackageTagsLabel"
            '
            'PackageTags
            '
            resources.ApplyResources(Me.PackageTags, "PackageTags")
            Me.PackageTags.Name = "PackageTags"
            '
            'PackageReleaseNotesLabel
            '
            resources.ApplyResources(Me.PackageReleaseNotesLabel, "PackageReleaseNotesLabel")
            Me.PackageReleaseNotesLabel.Name = "PackageReleaseNotesLabel"
            '
            'PackageReleaseNotes
            '
            resources.ApplyResources(Me.PackageReleaseNotes, "PackageReleaseNotes")
            Me.PackageReleaseNotes.Name = "PackageReleaseNotes"
            '
            'NeutralLanguageLabel
            '
            resources.ApplyResources(Me.NeutralLanguageLabel, "NeutralLanguageLabel")
            Me.NeutralLanguageLabel.Name = "NeutralLanguageLabel"
            '
            'NeutralLanguageComboBox
            '
            resources.ApplyResources(Me.NeutralLanguageComboBox, "NeutralLanguageComboBox")
            Me.NeutralLanguageComboBox.FormattingEnabled = True
            Me.NeutralLanguageComboBox.Name = "NeutralLanguageComboBox"
            '
            'AssemblyVersionLabel
            '
            resources.ApplyResources(Me.AssemblyVersionLabel, "AssemblyVersionLabel")
            Me.AssemblyVersionLabel.Name = "AssemblyVersionLabel"
            '
            'AssemblyVersionLayoutPanel
            '
            resources.ApplyResources(Me.AssemblyVersionLayoutPanel, "AssemblyVersionLayoutPanel")
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionRevisionTextBox, 3, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionBuildTextBox, 2, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionMinorTextBox, 1, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionMajorTextBox, 0, 0)
            Me.AssemblyVersionLayoutPanel.Name = "AssemblyVersionLayoutPanel"
            '
            'AssemblyVersionRevisionTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionRevisionTextBox, "AssemblyVersionRevisionTextBox")
            Me.AssemblyVersionRevisionTextBox.Name = "AssemblyVersionRevisionTextBox"
            '
            'AssemblyVersionBuildTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionBuildTextBox, "AssemblyVersionBuildTextBox")
            Me.AssemblyVersionBuildTextBox.Name = "AssemblyVersionBuildTextBox"
            '
            'AssemblyVersionMinorTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionMinorTextBox, "AssemblyVersionMinorTextBox")
            Me.AssemblyVersionMinorTextBox.Name = "AssemblyVersionMinorTextBox"
            '
            'AssemblyVersionMajorTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionMajorTextBox, "AssemblyVersionMajorTextBox")
            Me.AssemblyVersionMajorTextBox.Name = "AssemblyVersionMajorTextBox"
            '
            'AssemblyFileVersionLabel
            '
            resources.ApplyResources(Me.AssemblyFileVersionLabel, "AssemblyFileVersionLabel")
            Me.AssemblyFileVersionLabel.Name = "AssemblyFileVersionLabel"
            '
            'FileVersionLayoutPanel
            '
            resources.ApplyResources(Me.FileVersionLayoutPanel, "FileVersionLayoutPanel")
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionRevisionTextBox, 3, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionBuildTextBox, 2, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionMinorTextBox, 1, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionMajorTextBox, 0, 0)
            Me.FileVersionLayoutPanel.Name = "FileVersionLayoutPanel"
            '
            'FileVersionRevisionTextBox
            '
            resources.ApplyResources(Me.FileVersionRevisionTextBox, "FileVersionRevisionTextBox")
            Me.FileVersionRevisionTextBox.Name = "FileVersionRevisionTextBox"
            '
            'FileVersionBuildTextBox
            '
            resources.ApplyResources(Me.FileVersionBuildTextBox, "FileVersionBuildTextBox")
            Me.FileVersionBuildTextBox.Name = "FileVersionBuildTextBox"
            '
            'FileVersionMinorTextBox
            '
            resources.ApplyResources(Me.FileVersionMinorTextBox, "FileVersionMinorTextBox")
            Me.FileVersionMinorTextBox.Name = "FileVersionMinorTextBox"
            '
            'FileVersionMajorTextBox
            '
            resources.ApplyResources(Me.FileVersionMajorTextBox, "FileVersionMajorTextBox")
            Me.FileVersionMajorTextBox.Name = "FileVersionMajorTextBox"
            '
            'PackageIconLineLabel
            '
            Me.PackageIconLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.PackageIconLineLabel, "PackageIconLineLabel")
            Me.PackageIconLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.PackageIconLineLabel.Name = "PackageIconLineLabel"
            '
            'PackageIconUrlWarning
            '
            Me.PackageIconUrlWarning.BackColor = System.Drawing.SystemColors.Control
            Me.PackageIconUrlWarning.BorderStyle = System.Windows.Forms.BorderStyle.None
            resources.ApplyResources(Me.PackageIconUrlWarning, "PackageIconUrlWarning")
            Me.PackageIconUrlWarning.Name = "PackageIconUrlWarning"
            Me.PackageIconUrlWarning.ReadOnly = True
            '
            'LicenseLayoutPanel
            '
            resources.ApplyResources(Me.LicenseLayoutPanel, "LicenseLayoutPanel")
            Me.LicenseLayoutPanel.Name = "LicenseLayoutPanel"
            '
            'OrLabel
            '
            resources.ApplyResources(Me.OrLabel, "OrLabel")
            Me.OrLabel.Name = "OrLabel"
            '
            'PackagePropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.TableLayoutPanel)
            Me.Name = "PackagePropPage"
            Me.TableLayoutPanel.ResumeLayout(False)
            Me.TableLayoutPanel.PerformLayout()
            Me.ExpressionLayoutPanel.ResumeLayout(False)
            Me.ExpressionLayoutPanel.PerformLayout()
            Me.LicenseFileLayoutPanel.ResumeLayout(False)
            Me.LicenseFileLayoutPanel.PerformLayout()
            Me.IconFileLayoutPanel.ResumeLayout(False)
            Me.IconFileLayoutPanel.PerformLayout()
            Me.AssemblyVersionLayoutPanel.ResumeLayout(False)
            Me.AssemblyVersionLayoutPanel.PerformLayout()
            Me.FileVersionLayoutPanel.ResumeLayout(False)
            Me.FileVersionLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Friend WithEvents TableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionLabel As System.Windows.Forms.Label
        Friend WithEvents AssemblyCompanyLabel As System.Windows.Forms.Label
        Friend WithEvents CopyrightLabel As System.Windows.Forms.Label
        Friend WithEvents Copyright As System.Windows.Forms.TextBox
        Friend WithEvents DescriptionLabel As System.Windows.Forms.Label
        Friend WithEvents Description As System.Windows.Forms.TextBox
        Friend WithEvents PackageIcon As System.Windows.Forms.TextBox
        Friend WithEvents PackageIconLabel As System.Windows.Forms.Label
        Friend WithEvents PackageProjectUrl As System.Windows.Forms.TextBox
        Friend WithEvents PackageId As System.Windows.Forms.TextBox
        Friend WithEvents PackageIdLabel As System.Windows.Forms.Label
        Friend WithEvents PackageVersion As System.Windows.Forms.TextBox
        Friend WithEvents PackageVersionLabel As System.Windows.Forms.Label

        Friend WithEvents AuthorsLabel As System.Windows.Forms.Label
        Friend WithEvents PackageLicenseLabel As System.Windows.Forms.Label
        Friend WithEvents RepositoryUrl As System.Windows.Forms.TextBox
        Friend WithEvents RepositoryType As System.Windows.Forms.TextBox
        Friend WithEvents PackageTagsLabel As System.Windows.Forms.Label
        Friend WithEvents RepositoryUrlLabel As System.Windows.Forms.Label
        Friend WithEvents PackageReleaseNotesLabel As System.Windows.Forms.Label
        Friend WithEvents RepositoryTypeLabel As System.Windows.Forms.Label
        Friend WithEvents PackageTags As System.Windows.Forms.TextBox
        Friend WithEvents PackageReleaseNotes As System.Windows.Forms.TextBox
        Friend WithEvents PackageProjectUrlLabel As System.Windows.Forms.Label
        Friend WithEvents GeneratePackageOnBuild As System.Windows.Forms.CheckBox
        Friend WithEvents Authors As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyCompany As System.Windows.Forms.TextBox
        Friend WithEvents Product As System.Windows.Forms.TextBox
        Friend WithEvents ProductLabel As System.Windows.Forms.Label
        Friend WithEvents NeutralLanguageLabel As System.Windows.Forms.Label
        Friend WithEvents AssemblyVersionLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionRevisionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionBuildTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionMinorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionMajorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents NeutralLanguageComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents PackageRequireLicenseAcceptance As System.Windows.Forms.CheckBox
        Friend WithEvents FileVersionLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents FileVersionRevisionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionBuildTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionMinorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionMajorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyFileVersionLabel As System.Windows.Forms.Label
        Friend WithEvents LicenseLineLabel As System.Windows.Forms.Label
        Friend WithEvents LicenseUrlWarning As FixedWidthTextBox

        Friend WithEvents LicenseLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ExpressionLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents LicenseFileLayoutPanel As System.Windows.Forms.TableLayoutPanel

        Friend WithEvents IconFileLayoutPanel As System.Windows.Forms.TableLayoutPanel

        Friend WithEvents LicenseFileRadioButton As System.Windows.Forms.RadioButton
        Friend WithEvents LicenseExpressionRadioButton As System.Windows.Forms.RadioButton

        Friend WithEvents ExpressionLabel As System.Windows.Forms.Label
        Friend WithEvents PackageLicenseExpression As System.Windows.Forms.TextBox
        Friend WithEvents OrLabel As System.Windows.Forms.Label
        Friend WithEvents FileLabel As System.Windows.Forms.Label
        Friend WithEvents LicenseFileNameTextBox As System.Windows.Forms.TextBox
        Friend WithEvents LicenseBrowseButton As System.Windows.Forms.Button
        Friend WithEvents IconFileBrowseButton As System.Windows.Forms.Button
        Friend WithEvents PackageIconLineLabel As System.Windows.Forms.Label
        Friend WithEvents PackageIconUrlWarning As FixedWidthTextBox
    End Class

End Namespace
