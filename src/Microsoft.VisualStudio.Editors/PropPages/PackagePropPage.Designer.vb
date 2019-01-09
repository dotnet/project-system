' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Me.AssemblyVersionLabel = New System.Windows.Forms.Label()
            Me.CopyrightLabel = New System.Windows.Forms.Label()
            Me.Copyright = New System.Windows.Forms.TextBox()
            Me.DescriptionLabel = New System.Windows.Forms.Label()
            Me.Description = New System.Windows.Forms.TextBox()
            Me.PackageIconUrl = New System.Windows.Forms.TextBox()
            Me.PackageIconUrlLabel = New System.Windows.Forms.Label()
            Me.PackageProjectUrl = New System.Windows.Forms.TextBox()
            Me.PackageVersion = New System.Windows.Forms.TextBox()
            Me.PackageVersionLabel = New System.Windows.Forms.Label()
            Me.PackageLicenseUrlLabel = New System.Windows.Forms.Label()
            Me.RepositoryUrl = New System.Windows.Forms.TextBox()
            Me.RepositoryType = New System.Windows.Forms.TextBox()
            Me.PackageTagsLabel = New System.Windows.Forms.Label()
            Me.RepositoryUrlLabel = New System.Windows.Forms.Label()
            Me.PackageReleaseNotesLabel = New System.Windows.Forms.Label()
            Me.RepositoryTypeLabel = New System.Windows.Forms.Label()
            Me.PackageTags = New System.Windows.Forms.TextBox()
            Me.PackageReleaseNotes = New System.Windows.Forms.TextBox()
            Me.PackageProjectUrlLabel = New System.Windows.Forms.Label()
            Me.GeneratePackageOnBuild = New System.Windows.Forms.CheckBox()
            Me.NeutralLanguageLabel = New System.Windows.Forms.Label()
            Me.AssemblyVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.AssemblyVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.FileVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.PackageLicenseUrl = New System.Windows.Forms.TextBox()
            Me.NeutralLanguageComboBox = New System.Windows.Forms.ComboBox()
            Me.PackageRequireLicenseAcceptance = New System.Windows.Forms.CheckBox()
            Me.PackageIdLabel = New System.Windows.Forms.Label()
            Me.PackageId = New System.Windows.Forms.TextBox()
            Me.AuthorsLabel = New System.Windows.Forms.Label()
            Me.Authors = New System.Windows.Forms.TextBox()
            Me.AssemblyCompanyLabel = New System.Windows.Forms.Label()
            Me.ProductLabel = New System.Windows.Forms.Label()
            Me.AssemblyCompany = New System.Windows.Forms.TextBox()
            Me.Product = New System.Windows.Forms.TextBox()
            Me.AssemblyFileVersionLabel = New System.Windows.Forms.Label()
            Me.TableLayoutPanel.SuspendLayout()
            Me.AssemblyVersionLayoutPanel.SuspendLayout()
            Me.FileVersionLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TableLayoutPanel
            '
            resources.ApplyResources(Me.TableLayoutPanel, "TableLayoutPanel")
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyVersionLabel, 0, 25)
            Me.TableLayoutPanel.Controls.Add(Me.CopyrightLabel, 0, 10)
            Me.TableLayoutPanel.Controls.Add(Me.Copyright, 1, 10)
            Me.TableLayoutPanel.Controls.Add(Me.DescriptionLabel, 0, 9)
            Me.TableLayoutPanel.Controls.Add(Me.Description, 1, 9)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIconUrl, 1, 15)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIconUrlLabel, 0, 15)
            Me.TableLayoutPanel.Controls.Add(Me.PackageProjectUrl, 1, 14)
            Me.TableLayoutPanel.Controls.Add(Me.PackageVersion, 1, 4)
            Me.TableLayoutPanel.Controls.Add(Me.PackageVersionLabel, 0, 4)
            Me.TableLayoutPanel.Controls.Add(Me.PackageLicenseUrlLabel, 0, 13)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryUrl, 1, 16)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryType, 1, 17)
            Me.TableLayoutPanel.Controls.Add(Me.PackageTagsLabel, 0, 18)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryUrlLabel, 0, 16)
            Me.TableLayoutPanel.Controls.Add(Me.PackageReleaseNotesLabel, 0, 19)
            Me.TableLayoutPanel.Controls.Add(Me.RepositoryTypeLabel, 0, 17)
            Me.TableLayoutPanel.Controls.Add(Me.PackageTags, 1, 18)
            Me.TableLayoutPanel.Controls.Add(Me.PackageReleaseNotes, 1, 19)
            Me.TableLayoutPanel.Controls.Add(Me.PackageProjectUrlLabel, 0, 14)
            Me.TableLayoutPanel.Controls.Add(Me.GeneratePackageOnBuild, 0, 0)
            Me.TableLayoutPanel.Controls.Add(Me.NeutralLanguageLabel, 0, 24)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyVersionLayoutPanel, 1, 25)
            Me.TableLayoutPanel.Controls.Add(Me.FileVersionLayoutPanel, 1, 26)
            Me.TableLayoutPanel.Controls.Add(Me.PackageLicenseUrl, 1, 13)
            Me.TableLayoutPanel.Controls.Add(Me.NeutralLanguageComboBox, 1, 24)
            Me.TableLayoutPanel.Controls.Add(Me.PackageRequireLicenseAcceptance, 0, 1)
            Me.TableLayoutPanel.Controls.Add(Me.PackageIdLabel, 0, 2)
            Me.TableLayoutPanel.Controls.Add(Me.PackageId, 1, 2)
            Me.TableLayoutPanel.Controls.Add(Me.AuthorsLabel, 0, 5)
            Me.TableLayoutPanel.Controls.Add(Me.Authors, 1, 5)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyCompanyLabel, 0, 6)
            Me.TableLayoutPanel.Controls.Add(Me.ProductLabel, 0, 7)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyCompany, 1, 6)
            Me.TableLayoutPanel.Controls.Add(Me.Product, 1, 7)
            Me.TableLayoutPanel.Controls.Add(Me.AssemblyFileVersionLabel, 0, 26)
            Me.TableLayoutPanel.Name = "TableLayoutPanel"
            '
            'AssemblyVersionLabel
            '
            resources.ApplyResources(Me.AssemblyVersionLabel, "AssemblyVersionLabel")
            Me.AssemblyVersionLabel.Name = "AssemblyVersionLabel"
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
            'PackageIconUrl
            '
            resources.ApplyResources(Me.PackageIconUrl, "PackageIconUrl")
            Me.PackageIconUrl.Name = "PackageIconUrl"
            '
            'PackageIconUrlLabel
            '
            resources.ApplyResources(Me.PackageIconUrlLabel, "PackageIconUrlLabel")
            Me.PackageIconUrlLabel.Name = "PackageIconUrlLabel"
            '
            'PackageProjectUrl
            '
            resources.ApplyResources(Me.PackageProjectUrl, "PackageProjectUrl")
            Me.PackageProjectUrl.Name = "PackageProjectUrl"
            '
            'PackageVersion
            '
            resources.ApplyResources(Me.PackageVersion, "PackageVersion")
            Me.PackageVersion.Name = "PackageVersion"
            '
            'PackageVersionLabel
            '
            resources.ApplyResources(Me.PackageVersionLabel, "PackageVersionLabel")
            Me.PackageVersionLabel.Name = "PackageVersionLabel"
            '
            'PackageLicenseUrlLabel
            '
            resources.ApplyResources(Me.PackageLicenseUrlLabel, "PackageLicenseUrlLabel")
            Me.PackageLicenseUrlLabel.Name = "PackageLicenseUrlLabel"
            '
            'RepositoryUrl
            '
            resources.ApplyResources(Me.RepositoryUrl, "RepositoryUrl")
            Me.RepositoryUrl.Name = "RepositoryUrl"
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
            'RepositoryUrlLabel
            '
            resources.ApplyResources(Me.RepositoryUrlLabel, "RepositoryUrlLabel")
            Me.RepositoryUrlLabel.Name = "RepositoryUrlLabel"
            '
            'PackageReleaseNotesLabel
            '
            resources.ApplyResources(Me.PackageReleaseNotesLabel, "PackageReleaseNotesLabel")
            Me.PackageReleaseNotesLabel.Name = "PackageReleaseNotesLabel"
            '
            'RepositoryTypeLabel
            '
            resources.ApplyResources(Me.RepositoryTypeLabel, "RepositoryTypeLabel")
            Me.RepositoryTypeLabel.Name = "RepositoryTypeLabel"
            '
            'PackageTags
            '
            resources.ApplyResources(Me.PackageTags, "PackageTags")
            Me.PackageTags.Name = "PackageTags"
            '
            'PackageReleaseNotes
            '
            resources.ApplyResources(Me.PackageReleaseNotes, "PackageReleaseNotes")
            Me.PackageReleaseNotes.Name = "PackageReleaseNotes"
            '
            'PackageProjectUrlLabel
            '
            resources.ApplyResources(Me.PackageProjectUrlLabel, "PackageProjectUrlLabel")
            Me.PackageProjectUrlLabel.Name = "PackageProjectUrlLabel"
            '
            'GeneratePackageOnBuild
            '
            resources.ApplyResources(Me.GeneratePackageOnBuild, "GeneratePackageOnBuild")
            Me.TableLayoutPanel.SetColumnSpan(Me.GeneratePackageOnBuild, 2)
            Me.GeneratePackageOnBuild.Name = "GeneratePackageOnBuild"
            Me.GeneratePackageOnBuild.UseVisualStyleBackColor = True
            '
            'NeutralLanguageLabel
            '
            resources.ApplyResources(Me.NeutralLanguageLabel, "NeutralLanguageLabel")
            Me.NeutralLanguageLabel.Name = "NeutralLanguageLabel"
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
            'PackageLicenseUrl
            '
            resources.ApplyResources(Me.PackageLicenseUrl, "PackageLicenseUrl")
            Me.PackageLicenseUrl.Name = "PackageLicenseUrl"
            '
            'NeutralLanguageComboBox
            '
            Me.NeutralLanguageComboBox.FormattingEnabled = True
            resources.ApplyResources(Me.NeutralLanguageComboBox, "NeutralLanguageComboBox")
            Me.NeutralLanguageComboBox.Name = "NeutralLanguageComboBox"
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
            'ProductLabel
            '
            resources.ApplyResources(Me.ProductLabel, "ProductLabel")
            Me.ProductLabel.Name = "ProductLabel"
            '
            'AssemblyCompany
            '
            resources.ApplyResources(Me.AssemblyCompany, "AssemblyCompany")
            Me.AssemblyCompany.Name = "AssemblyCompany"
            '
            'Product
            '
            resources.ApplyResources(Me.Product, "Product")
            Me.Product.Name = "Product"
            '
            'AssemblyFileVersionLabel
            '
            resources.ApplyResources(Me.AssemblyFileVersionLabel, "AssemblyFileVersionLabel")
            Me.AssemblyFileVersionLabel.Name = "AssemblyFileVersionLabel"
            '
            'PackagePropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.TableLayoutPanel)
            Me.Name = "PackagePropPage"
            Me.TableLayoutPanel.ResumeLayout(False)
            Me.TableLayoutPanel.PerformLayout()
            Me.AssemblyVersionLayoutPanel.ResumeLayout(False)
            Me.AssemblyVersionLayoutPanel.PerformLayout()
            Me.FileVersionLayoutPanel.ResumeLayout(False)
            Me.FileVersionLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Friend WithEvents TableLayoutPanel As Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionLabel As Windows.Forms.Label
        Friend WithEvents AssemblyCompanyLabel As Windows.Forms.Label
        Friend WithEvents CopyrightLabel As Windows.Forms.Label
        Friend WithEvents Copyright As Windows.Forms.TextBox
        Friend WithEvents DescriptionLabel As Windows.Forms.Label
        Friend WithEvents Description As Windows.Forms.TextBox
        Friend WithEvents PackageIconUrl As Windows.Forms.TextBox
        Friend WithEvents PackageIconUrlLabel As Windows.Forms.Label
        Friend WithEvents PackageProjectUrl As Windows.Forms.TextBox
        Friend WithEvents PackageId As Windows.Forms.TextBox
        Friend WithEvents PackageIdLabel As Windows.Forms.Label
        Friend WithEvents PackageVersion As Windows.Forms.TextBox
        Friend WithEvents PackageVersionLabel As Windows.Forms.Label
        Friend WithEvents PackageLicenseUrl As Windows.Forms.TextBox
        Friend WithEvents AuthorsLabel As Windows.Forms.Label
        Friend WithEvents PackageLicenseUrlLabel As Windows.Forms.Label
        Friend WithEvents RepositoryUrl As Windows.Forms.TextBox
        Friend WithEvents RepositoryType As Windows.Forms.TextBox
        Friend WithEvents PackageTagsLabel As Windows.Forms.Label
        Friend WithEvents RepositoryUrlLabel As Windows.Forms.Label
        Friend WithEvents PackageReleaseNotesLabel As Windows.Forms.Label
        Friend WithEvents RepositoryTypeLabel As Windows.Forms.Label
        Friend WithEvents PackageTags As Windows.Forms.TextBox
        Friend WithEvents PackageReleaseNotes As Windows.Forms.TextBox
        Friend WithEvents PackageProjectUrlLabel As Windows.Forms.Label
        Friend WithEvents GeneratePackageOnBuild As Windows.Forms.CheckBox
        Friend WithEvents Authors As Windows.Forms.TextBox
        Friend WithEvents AssemblyCompany As Windows.Forms.TextBox
        Friend WithEvents Product As Windows.Forms.TextBox
        Friend WithEvents ProductLabel As Windows.Forms.Label
        Friend WithEvents NeutralLanguageLabel As Windows.Forms.Label
        Friend WithEvents AssemblyVersionLayoutPanel As Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionRevisionTextBox As Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionBuildTextBox As Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionMinorTextBox As Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionMajorTextBox As Windows.Forms.TextBox
        Friend WithEvents NeutralLanguageComboBox As Windows.Forms.ComboBox
        Friend WithEvents PackageRequireLicenseAcceptance As Windows.Forms.CheckBox
        Friend WithEvents FileVersionLayoutPanel As Windows.Forms.TableLayoutPanel
        Friend WithEvents FileVersionRevisionTextBox As Windows.Forms.TextBox
        Friend WithEvents FileVersionBuildTextBox As Windows.Forms.TextBox
        Friend WithEvents FileVersionMinorTextBox As Windows.Forms.TextBox
        Friend WithEvents FileVersionMajorTextBox As Windows.Forms.TextBox
        Friend WithEvents AssemblyFileVersionLabel As Windows.Forms.Label
    End Class

End Namespace
