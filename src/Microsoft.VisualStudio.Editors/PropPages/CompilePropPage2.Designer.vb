' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class CompilePropPage2

        Friend WithEvents BuildOutputPathLabel As System.Windows.Forms.Label
        Friend WithEvents BuildOutputPathTextBox As System.Windows.Forms.TextBox
        Friend WithEvents BuildOutputPathButton As System.Windows.Forms.Button
        Friend WithEvents AdvancedOptionsButton As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents buildOutputTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents CompileOptionsGroupBox As System.Windows.Forms.GroupBox
        Friend WithEvents CompileOptionsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents OptionExplicitLabel As System.Windows.Forms.Label
        Friend WithEvents DisableAllWarningsCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents WarningsAsErrorCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents OptionExplicitComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents BuildEventsButton As System.Windows.Forms.Button
        Friend WithEvents RegisterForComInteropCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents OptionCompareComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents GenerateXMLCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents OptionStrictLabel As System.Windows.Forms.Label
        Friend WithEvents OptionStrictComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents OptionCompareLabel As System.Windows.Forms.Label
        Friend WithEvents OptionInferLabel As System.Windows.Forms.Label
        Friend WithEvents OptionInferComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents WarningsConfigurationsGridViewLabel As System.Windows.Forms.Label
        Friend WithEvents WarningsGridView As Microsoft.VisualStudio.Editors.PropertyPages.CompilePropPage2.InternalDataGridView
        Friend WithEvents ConditionColumn As System.Windows.Forms.DataGridViewTextBoxColumn
        Friend WithEvents NotificationColumn As System.Windows.Forms.DataGridViewComboBoxColumn
        Friend WithEvents TargetCPULabel As System.Windows.Forms.Label
        Friend WithEvents TargetCPUComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents Prefer32BitCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents AdvancedCompileOptionsLabelLine As System.Windows.Forms.Label
        Private _components As System.ComponentModel.IContainer
        'put this bock in
        'Me.WarningsGridView = New Microsoft.VisualStudio.Editors.PropertyPages.CompilePropPage2.InternalDataGridView

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(CompilePropPage2))
            Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
            Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
            Me.BuildOutputPathLabel = New System.Windows.Forms.Label()
            Me.BuildOutputPathTextBox = New System.Windows.Forms.TextBox()
            Me.BuildOutputPathButton = New System.Windows.Forms.Button()
            Me.AdvancedOptionsButton = New System.Windows.Forms.Button()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.buildOutputTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.CompileOptionsGroupBox = New System.Windows.Forms.GroupBox()
            Me.CompileOptionsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.AdvancedCompileOptionsLabelLine = New System.Windows.Forms.Label()
            Me.OptionExplicitLabel = New System.Windows.Forms.Label()
            Me.DisableAllWarningsCheckBox = New System.Windows.Forms.CheckBox()
            Me.WarningsAsErrorCheckBox = New System.Windows.Forms.CheckBox()
            Me.OptionExplicitComboBox = New System.Windows.Forms.ComboBox()
            Me.BuildEventsButton = New System.Windows.Forms.Button()
            Me.RegisterForComInteropCheckBox = New System.Windows.Forms.CheckBox()
            Me.OptionCompareComboBox = New System.Windows.Forms.ComboBox()
            Me.GenerateXMLCheckBox = New System.Windows.Forms.CheckBox()
            Me.OptionStrictLabel = New System.Windows.Forms.Label()
            Me.OptionStrictComboBox = New System.Windows.Forms.ComboBox()
            Me.OptionCompareLabel = New System.Windows.Forms.Label()
            Me.OptionInferLabel = New System.Windows.Forms.Label()
            Me.OptionInferComboBox = New System.Windows.Forms.ComboBox()
            Me.WarningsConfigurationsGridViewLabel = New System.Windows.Forms.Label()
            Me.WarningsGridView = New Microsoft.VisualStudio.Editors.PropertyPages.CompilePropPage2.InternalDataGridView()
            Me.ConditionColumn = New System.Windows.Forms.DataGridViewTextBoxColumn()
            Me.NotificationColumn = New System.Windows.Forms.DataGridViewComboBoxColumn()
            Me.TargetCPULabel = New System.Windows.Forms.Label()
            Me.TargetCPUComboBox = New System.Windows.Forms.ComboBox()
            Me.Prefer32BitCheckBox = New System.Windows.Forms.CheckBox()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.buildOutputTableLayoutPanel.SuspendLayout()
            Me.CompileOptionsGroupBox.SuspendLayout()
            Me.CompileOptionsTableLayoutPanel.SuspendLayout()
            CType(Me.WarningsGridView, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'BuildOutputPathLabel
            '
            resources.ApplyResources(Me.BuildOutputPathLabel, "BuildOutputPathLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.BuildOutputPathLabel, 2)
            Me.BuildOutputPathLabel.Name = "BuildOutputPathLabel"
            '
            'BuildOutputPathTextBox
            '
            resources.ApplyResources(Me.BuildOutputPathTextBox, "BuildOutputPathTextBox")
            Me.BuildOutputPathTextBox.Name = "BuildOutputPathTextBox"
            '
            'BuildOutputPathButton
            '
            resources.ApplyResources(Me.BuildOutputPathButton, "BuildOutputPathButton")
            Me.BuildOutputPathButton.Name = "BuildOutputPathButton"
            '
            'AdvancedOptionsButton
            '
            resources.ApplyResources(Me.AdvancedOptionsButton, "AdvancedOptionsButton")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.AdvancedOptionsButton, 2)
            Me.AdvancedOptionsButton.Name = "AdvancedOptionsButton"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.BuildOutputPathLabel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.buildOutputTableLayoutPanel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CompileOptionsGroupBox, 0, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'buildOutputTableLayoutPanel
            '
            resources.ApplyResources(Me.buildOutputTableLayoutPanel, "buildOutputTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.buildOutputTableLayoutPanel, 2)
            Me.buildOutputTableLayoutPanel.Controls.Add(Me.BuildOutputPathTextBox, 0, 0)
            Me.buildOutputTableLayoutPanel.Controls.Add(Me.BuildOutputPathButton, 1, 0)
            Me.buildOutputTableLayoutPanel.Name = "buildOutputTableLayoutPanel"
            '
            'CompileOptionsGroupBox
            '
            resources.ApplyResources(Me.CompileOptionsGroupBox, "CompileOptionsGroupBox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.CompileOptionsGroupBox, 2)
            Me.CompileOptionsGroupBox.Controls.Add(Me.CompileOptionsTableLayoutPanel)
            Me.CompileOptionsGroupBox.Name = "CompileOptionsGroupBox"
            Me.CompileOptionsGroupBox.TabStop = False
            '
            'CompileOptionsTableLayoutPanel
            '
            resources.ApplyResources(Me.CompileOptionsTableLayoutPanel, "CompileOptionsTableLayoutPanel")
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.AdvancedCompileOptionsLabelLine, 0, 14)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionExplicitLabel, 0, 0)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.DisableAllWarningsCheckBox, 0, 10)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.WarningsAsErrorCheckBox, 0, 11)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionExplicitComboBox, 0, 1)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.BuildEventsButton, 1, 13)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.RegisterForComInteropCheckBox, 0, 13)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionCompareComboBox, 0, 3)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.GenerateXMLCheckBox, 0, 12)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionStrictLabel, 1, 0)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionStrictComboBox, 1, 1)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionCompareLabel, 0, 2)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionInferLabel, 1, 2)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.OptionInferComboBox, 1, 3)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.WarningsConfigurationsGridViewLabel, 0, 7)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.WarningsGridView, 0, 8)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.AdvancedOptionsButton, 0, 15)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.TargetCPULabel, 0, 4)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.TargetCPUComboBox, 0, 5)
            Me.CompileOptionsTableLayoutPanel.Controls.Add(Me.Prefer32BitCheckBox, 0, 6)
            Me.CompileOptionsTableLayoutPanel.Name = "CompileOptionsTableLayoutPanel"
            '
            'AdvancedCompileOptionsLabelLine
            '
            Me.AdvancedCompileOptionsLabelLine.AccessibleRole = System.Windows.Forms.AccessibleRole.Graphic
            resources.ApplyResources(Me.AdvancedCompileOptionsLabelLine, "AdvancedCompileOptionsLabelLine")
            Me.AdvancedCompileOptionsLabelLine.BackColor = System.Drawing.SystemColors.ControlDark
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.AdvancedCompileOptionsLabelLine, 2)
            Me.AdvancedCompileOptionsLabelLine.Name = "AdvancedCompileOptionsLabelLine"
            '
            'OptionExplicitLabel
            '
            resources.ApplyResources(Me.OptionExplicitLabel, "OptionExplicitLabel")
            Me.OptionExplicitLabel.Name = "OptionExplicitLabel"
            '
            'DisableAllWarningsCheckBox
            '
            resources.ApplyResources(Me.DisableAllWarningsCheckBox, "DisableAllWarningsCheckBox")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.DisableAllWarningsCheckBox, 2)
            Me.DisableAllWarningsCheckBox.Name = "DisableAllWarningsCheckBox"
            '
            'WarningsAsErrorCheckBox
            '
            resources.ApplyResources(Me.WarningsAsErrorCheckBox, "WarningsAsErrorCheckBox")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.WarningsAsErrorCheckBox, 2)
            Me.WarningsAsErrorCheckBox.Name = "WarningsAsErrorCheckBox"
            '
            'OptionExplicitComboBox
            '
            resources.ApplyResources(Me.OptionExplicitComboBox, "OptionExplicitComboBox")
            Me.OptionExplicitComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionExplicitComboBox.FormattingEnabled = True
            Me.OptionExplicitComboBox.Name = "OptionExplicitComboBox"
            '
            'BuildEventsButton
            '
            resources.ApplyResources(Me.BuildEventsButton, "BuildEventsButton")
            Me.BuildEventsButton.Name = "BuildEventsButton"
            '
            'RegisterForComInteropCheckBox
            '
            resources.ApplyResources(Me.RegisterForComInteropCheckBox, "RegisterForComInteropCheckBox")
            Me.RegisterForComInteropCheckBox.Name = "RegisterForComInteropCheckBox"
            '
            'OptionCompareComboBox
            '
            resources.ApplyResources(Me.OptionCompareComboBox, "OptionCompareComboBox")
            Me.OptionCompareComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionCompareComboBox.FormattingEnabled = True
            Me.OptionCompareComboBox.Name = "OptionCompareComboBox"
            '
            'GenerateXMLCheckBox
            '
            resources.ApplyResources(Me.GenerateXMLCheckBox, "GenerateXMLCheckBox")
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.GenerateXMLCheckBox, 2)
            Me.GenerateXMLCheckBox.Name = "GenerateXMLCheckBox"
            '
            'OptionStrictLabel
            '
            resources.ApplyResources(Me.OptionStrictLabel, "OptionStrictLabel")
            Me.OptionStrictLabel.Name = "OptionStrictLabel"
            '
            'OptionStrictComboBox
            '
            resources.ApplyResources(Me.OptionStrictComboBox, "OptionStrictComboBox")
            Me.OptionStrictComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionStrictComboBox.FormattingEnabled = True
            Me.OptionStrictComboBox.Name = "OptionStrictComboBox"
            '
            'OptionCompareLabel
            '
            resources.ApplyResources(Me.OptionCompareLabel, "OptionCompareLabel")
            Me.OptionCompareLabel.Name = "OptionCompareLabel"
            '
            'OptionInferLabel
            '
            resources.ApplyResources(Me.OptionInferLabel, "OptionInferLabel")
            Me.OptionInferLabel.Name = "OptionInferLabel"
            '
            'OptionInferComboBox
            '
            resources.ApplyResources(Me.OptionInferComboBox, "OptionInferComboBox")
            Me.OptionInferComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.OptionInferComboBox.FormattingEnabled = True
            Me.OptionInferComboBox.Name = "OptionInferComboBox"
            '
            'WarningsConfigurationsGridViewLabel
            '
            resources.ApplyResources(Me.WarningsConfigurationsGridViewLabel, "WarningsConfigurationsGridViewLabel")
            Me.WarningsConfigurationsGridViewLabel.Name = "WarningsConfigurationsGridViewLabel"
            '
            'WarningsGridView
            '
            Me.WarningsGridView.AllowUserToAddRows = False
            Me.WarningsGridView.AllowUserToDeleteRows = False
            Me.WarningsGridView.AllowUserToResizeRows = False
            resources.ApplyResources(Me.WarningsGridView, "WarningsGridView")
            Me.WarningsGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells
            Me.WarningsGridView.BackgroundColor = System.Drawing.SystemColors.Window
            Me.WarningsGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
            Me.WarningsGridView.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ConditionColumn, Me.NotificationColumn})
            Me.CompileOptionsTableLayoutPanel.SetColumnSpan(Me.WarningsGridView, 2)
            Me.WarningsGridView.MultiSelect = False
            Me.WarningsGridView.Name = "WarningsGridView"
            Me.WarningsGridView.RowHeadersVisible = False
            Me.CompileOptionsTableLayoutPanel.SetRowSpan(Me.WarningsGridView, 2)
            '
            'ConditionColumn
            '
            Me.ConditionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
            DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
            Me.ConditionColumn.DefaultCellStyle = DataGridViewCellStyle1
            Me.ConditionColumn.FillWeight = 65.0!
            resources.ApplyResources(Me.ConditionColumn, "ConditionColumn")
            Me.ConditionColumn.Name = "ConditionColumn"
            Me.ConditionColumn.ReadOnly = True
            Me.ConditionColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
            '
            'NotificationColumn
            '
            Me.NotificationColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
            DataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
            Me.NotificationColumn.DefaultCellStyle = DataGridViewCellStyle2
            Me.NotificationColumn.FillWeight = 35.0!
            resources.ApplyResources(Me.NotificationColumn, "NotificationColumn")
            Me.NotificationColumn.Name = "NotificationColumn"
            '
            'TargetCPULabel
            '
            resources.ApplyResources(Me.TargetCPULabel, "TargetCPULabel")
            Me.TargetCPULabel.Name = "TargetCPULabel"
            '
            'TargetCPUComboBox
            '
            resources.ApplyResources(Me.TargetCPUComboBox, "TargetCPUComboBox")
            Me.TargetCPUComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TargetCPUComboBox.FormattingEnabled = True
            Me.TargetCPUComboBox.Name = "TargetCPUComboBox"
            '
            'Prefer32BitCheckBox
            '
            resources.ApplyResources(Me.Prefer32BitCheckBox, "Prefer32BitCheckBox")
            Me.Prefer32BitCheckBox.Name = "Prefer32BitCheckBox"
            '
            'CompilePropPage2
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "CompilePropPage2"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.buildOutputTableLayoutPanel.ResumeLayout(False)
            Me.buildOutputTableLayoutPanel.PerformLayout()
            Me.CompileOptionsGroupBox.ResumeLayout(False)
            Me.CompileOptionsGroupBox.PerformLayout()
            Me.CompileOptionsTableLayoutPanel.ResumeLayout(False)
            Me.CompileOptionsTableLayoutPanel.PerformLayout()
            CType(Me.WarningsGridView, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)

        End Sub

    End Class

End Namespace
