' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class AssemblyInfoPropPage

        Friend WithEvents Title As System.Windows.Forms.TextBox
        Friend WithEvents Description As System.Windows.Forms.TextBox
        Friend WithEvents Company As System.Windows.Forms.TextBox
        Friend WithEvents Product As System.Windows.Forms.TextBox
        Friend WithEvents Copyright As System.Windows.Forms.TextBox
        Friend WithEvents Trademark As System.Windows.Forms.TextBox
        Friend WithEvents TitleLabel As System.Windows.Forms.Label
        Friend WithEvents TrademarkLabel As System.Windows.Forms.Label
        Friend WithEvents CopyrightLabel As System.Windows.Forms.Label
        Friend WithEvents ProductLabel As System.Windows.Forms.Label
        Friend WithEvents CompanyLabel As System.Windows.Forms.Label
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionLabel As System.Windows.Forms.Label
        Friend WithEvents FileVersionLabel As System.Windows.Forms.Label
        Friend WithEvents ComVisibleCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents GuidLabel As System.Windows.Forms.Label
        Friend WithEvents GuidTextBox As System.Windows.Forms.TextBox
        Friend WithEvents NeutralLanguageLabel As System.Windows.Forms.Label
        Friend WithEvents AssemblyVersionLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionMajorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionMinorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionBuildTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionRevisionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents FileVersionMajorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionMinorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionBuildTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionRevisionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents DescriptionLabel As System.Windows.Forms.Label
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AssemblyInfoPropPage))
            Me.TitleLabel = New System.Windows.Forms.Label()
            Me.TrademarkLabel = New System.Windows.Forms.Label()
            Me.CopyrightLabel = New System.Windows.Forms.Label()
            Me.ProductLabel = New System.Windows.Forms.Label()
            Me.CompanyLabel = New System.Windows.Forms.Label()
            Me.DescriptionLabel = New System.Windows.Forms.Label()
            Me.Title = New System.Windows.Forms.TextBox()
            Me.Description = New System.Windows.Forms.TextBox()
            Me.Company = New System.Windows.Forms.TextBox()
            Me.Product = New System.Windows.Forms.TextBox()
            Me.Copyright = New System.Windows.Forms.TextBox()
            Me.Trademark = New System.Windows.Forms.TextBox()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.ComVisibleCheckBox = New System.Windows.Forms.CheckBox()
            Me.NeutralLanguageLabel = New System.Windows.Forms.Label()
            Me.AssemblyVersionLabel = New System.Windows.Forms.Label()
            Me.AssemblyVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.AssemblyVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionLabel = New System.Windows.Forms.Label()
            Me.FileVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.FileVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.GuidTextBox = New System.Windows.Forms.TextBox()
            Me.NeutralLanguageComboBox = New System.Windows.Forms.ComboBox()
            Me.GuidLabel = New System.Windows.Forms.Label()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.AssemblyVersionLayoutPanel.SuspendLayout()
            Me.FileVersionLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TitleLabel
            '
            resources.ApplyResources(Me.TitleLabel, "TitleLabel")
            Me.TitleLabel.Name = "TitleLabel"
            '
            'TrademarkLabel
            '
            resources.ApplyResources(Me.TrademarkLabel, "TrademarkLabel")
            Me.TrademarkLabel.Name = "TrademarkLabel"
            '
            'CopyrightLabel
            '
            resources.ApplyResources(Me.CopyrightLabel, "CopyrightLabel")
            Me.CopyrightLabel.Name = "CopyrightLabel"
            '
            'ProductLabel
            '
            resources.ApplyResources(Me.ProductLabel, "ProductLabel")
            Me.ProductLabel.Name = "ProductLabel"
            '
            'CompanyLabel
            '
            resources.ApplyResources(Me.CompanyLabel, "CompanyLabel")
            Me.CompanyLabel.Name = "CompanyLabel"
            '
            'DescriptionLabel
            '
            resources.ApplyResources(Me.DescriptionLabel, "DescriptionLabel")
            Me.DescriptionLabel.Name = "DescriptionLabel"
            '
            'Title
            '
            resources.ApplyResources(Me.Title, "Title")
            Me.Title.Name = "Title"
            '
            'Description
            '
            resources.ApplyResources(Me.Description, "Description")
            Me.Description.Name = "Description"
            '
            'Company
            '
            resources.ApplyResources(Me.Company, "Company")
            Me.Company.Name = "Company"
            '
            'Product
            '
            resources.ApplyResources(Me.Product, "Product")
            Me.Product.Name = "Product"
            '
            'Copyright
            '
            resources.ApplyResources(Me.Copyright, "Copyright")
            Me.Copyright.Name = "Copyright"
            '
            'Trademark
            '
            resources.ApplyResources(Me.Trademark, "Trademark")
            Me.Trademark.Name = "Trademark"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TitleLabel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ComVisibleCheckBox, 0, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.NeutralLanguageLabel, 0, 9)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Title, 1, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DescriptionLabel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Description, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CompanyLabel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Company, 1, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ProductLabel, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Product, 1, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CopyrightLabel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Copyright, 1, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TrademarkLabel, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Trademark, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.AssemblyVersionLabel, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.AssemblyVersionLayoutPanel, 1, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FileVersionLabel, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FileVersionLayoutPanel, 1, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GuidTextBox, 1, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.NeutralLanguageComboBox, 1, 9)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GuidLabel, 0, 8)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'ComVisibleCheckBox
            '
            resources.ApplyResources(Me.ComVisibleCheckBox, "ComVisibleCheckBox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.ComVisibleCheckBox, 2)
            Me.ComVisibleCheckBox.Name = "ComVisibleCheckBox"
            '
            'NeutralLanguageLabel
            '
            resources.ApplyResources(Me.NeutralLanguageLabel, "NeutralLanguageLabel")
            Me.NeutralLanguageLabel.Name = "NeutralLanguageLabel"
            '
            'AssemblyVersionLabel
            '
            resources.ApplyResources(Me.AssemblyVersionLabel, "AssemblyVersionLabel")
            Me.AssemblyVersionLabel.Name = "AssemblyVersionLabel"
            '
            'AssemblyVersionLayoutPanel
            '
            resources.ApplyResources(Me.AssemblyVersionLayoutPanel, "AssemblyVersionLayoutPanel")
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionMajorTextBox, 0, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionMinorTextBox, 1, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionBuildTextBox, 2, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionRevisionTextBox, 3, 0)
            Me.AssemblyVersionLayoutPanel.Name = "AssemblyVersionLayoutPanel"
            '
            'AssemblyVersionMajorTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionMajorTextBox, "AssemblyVersionMajorTextBox")
            Me.AssemblyVersionMajorTextBox.Name = "AssemblyVersionMajorTextBox"
            '
            'AssemblyVersionMinorTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionMinorTextBox, "AssemblyVersionMinorTextBox")
            Me.AssemblyVersionMinorTextBox.Name = "AssemblyVersionMinorTextBox"
            '
            'AssemblyVersionBuildTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionBuildTextBox, "AssemblyVersionBuildTextBox")
            Me.AssemblyVersionBuildTextBox.Name = "AssemblyVersionBuildTextBox"
            '
            'AssemblyVersionRevisionTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionRevisionTextBox, "AssemblyVersionRevisionTextBox")
            Me.AssemblyVersionRevisionTextBox.Name = "AssemblyVersionRevisionTextBox"
            '
            'FileVersionLabel
            '
            resources.ApplyResources(Me.FileVersionLabel, "FileVersionLabel")
            Me.FileVersionLabel.Name = "FileVersionLabel"
            '
            'FileVersionLayoutPanel
            '
            resources.ApplyResources(Me.FileVersionLayoutPanel, "FileVersionLayoutPanel")
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionMajorTextBox, 0, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionMinorTextBox, 1, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionBuildTextBox, 2, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionRevisionTextBox, 3, 0)
            Me.FileVersionLayoutPanel.Name = "FileVersionLayoutPanel"
            '
            'FileVersionMajorTextBox
            '
            resources.ApplyResources(Me.FileVersionMajorTextBox, "FileVersionMajorTextBox")
            Me.FileVersionMajorTextBox.Name = "FileVersionMajorTextBox"
            '
            'FileVersionMinorTextBox
            '
            resources.ApplyResources(Me.FileVersionMinorTextBox, "FileVersionMinorTextBox")
            Me.FileVersionMinorTextBox.Name = "FileVersionMinorTextBox"
            '
            'FileVersionBuildTextBox
            '
            resources.ApplyResources(Me.FileVersionBuildTextBox, "FileVersionBuildTextBox")
            Me.FileVersionBuildTextBox.Name = "FileVersionBuildTextBox"
            '
            'FileVersionRevisionTextBox
            '
            resources.ApplyResources(Me.FileVersionRevisionTextBox, "FileVersionRevisionTextBox")
            Me.FileVersionRevisionTextBox.Name = "FileVersionRevisionTextBox"
            '
            'GuidTextBox
            '
            resources.ApplyResources(Me.GuidTextBox, "GuidTextBox")
            Me.GuidTextBox.Name = "GuidTextBox"
            '
            'NeutralLanguageComboBox
            '
            Me.NeutralLanguageComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.NeutralLanguageComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
            resources.ApplyResources(Me.NeutralLanguageComboBox, "NeutralLanguageComboBox")
            Me.NeutralLanguageComboBox.FormattingEnabled = True
            Me.NeutralLanguageComboBox.Name = "NeutralLanguageComboBox"
            Me.NeutralLanguageComboBox.Sorted = True
            '
            'GuidLabel
            '
            resources.ApplyResources(Me.GuidLabel, "GuidLabel")
            Me.GuidLabel.Name = "GuidLabel"
            '
            'AssemblyInfoPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AssemblyInfoPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.AssemblyVersionLayoutPanel.ResumeLayout(False)
            Me.AssemblyVersionLayoutPanel.PerformLayout()
            Me.FileVersionLayoutPanel.ResumeLayout(False)
            Me.FileVersionLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
    End Class

End Namespace
