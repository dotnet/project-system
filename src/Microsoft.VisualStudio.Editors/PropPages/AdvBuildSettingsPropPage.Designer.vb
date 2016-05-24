' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Class AdvBuildSettingsPropPage

        Friend WithEvents lblLanguageVersion As System.Windows.Forms.Label
        Friend WithEvents lblReportCompilerErrors As System.Windows.Forms.Label
        Friend WithEvents chkOverflow As System.Windows.Forms.CheckBox
        Friend WithEvents cboLanguageVersion As System.Windows.Forms.ComboBox
        Friend WithEvents cboReportCompilerErrors As System.Windows.Forms.ComboBox
        Friend WithEvents lblDebugInfo As System.Windows.Forms.Label
        Friend WithEvents lblFileAlignment As System.Windows.Forms.Label
        Friend WithEvents lblDLLBase As System.Windows.Forms.Label
        Friend WithEvents cboDebugInfo As System.Windows.Forms.ComboBox
        Friend WithEvents cboFileAlignment As System.Windows.Forms.ComboBox
        Friend WithEvents txtDLLBase As System.Windows.Forms.TextBox
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalLabel As System.Windows.Forms.Label
        Friend WithEvents generalLineLabel As System.Windows.Forms.Label
        Friend WithEvents outputTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents outputLabel As System.Windows.Forms.Label
        Friend WithEvents outputLineLabel As System.Windows.Forms.Label
        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvBuildSettingsPropPage))
            Me.lblLanguageVersion = New System.Windows.Forms.Label
            Me.lblReportCompilerErrors = New System.Windows.Forms.Label
            Me.cboLanguageVersion = New System.Windows.Forms.ComboBox
            Me.cboReportCompilerErrors = New System.Windows.Forms.ComboBox
            Me.lblDebugInfo = New System.Windows.Forms.Label
            Me.cboDebugInfo = New System.Windows.Forms.ComboBox
            Me.lblFileAlignment = New System.Windows.Forms.Label
            Me.cboFileAlignment = New System.Windows.Forms.ComboBox
            Me.lblDLLBase = New System.Windows.Forms.Label
            Me.txtDLLBase = New System.Windows.Forms.TextBox
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.outputTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.outputLabel = New System.Windows.Forms.Label
            Me.outputLineLabel = New System.Windows.Forms.Label
            Me.generalTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.generalLabel = New System.Windows.Forms.Label
            Me.generalLineLabel = New System.Windows.Forms.Label
            Me.chkOverflow = New System.Windows.Forms.CheckBox
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.outputTableLayoutPanel.SuspendLayout()
            Me.generalTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'lblLanguageVersion
            '
            resources.ApplyResources(Me.lblLanguageVersion, "lblLanguageVersion")
            Me.lblLanguageVersion.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblLanguageVersion.Name = "lblLanguageVersion"
            '
            'cboLanguageVersion
            '
            resources.ApplyResources(Me.cboLanguageVersion, "cboLanguageVersion")
            Me.cboLanguageVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboLanguageVersion.FormattingEnabled = True
            Me.cboLanguageVersion.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboLanguageVersion.Name = "cboLanguageVersion"
            '
            'lblReportCompilerErrors
            '
            resources.ApplyResources(Me.lblReportCompilerErrors, "lblReportCompilerErrors")
            Me.lblReportCompilerErrors.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblReportCompilerErrors.Name = "lblReportCompilerErrors"
            '
            'cboReportCompilerErrors
            '
            resources.ApplyResources(Me.cboReportCompilerErrors, "cboReportCompilerErrors")
            Me.cboReportCompilerErrors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboReportCompilerErrors.FormattingEnabled = True
            Me.cboReportCompilerErrors.Items.AddRange(New Object() {"none", "prompt", "send", "queue"})
            Me.cboReportCompilerErrors.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboReportCompilerErrors.Name = "cboReportCompilerErrors"
            '
            'lblDebugInfo
            '
            resources.ApplyResources(Me.lblDebugInfo, "lblDebugInfo")
            Me.lblDebugInfo.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblDebugInfo.Name = "lblDebugInfo"
            '
            'cboDebugInfo
            '
            resources.ApplyResources(Me.cboDebugInfo, "cboDebugInfo")
            Me.cboDebugInfo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboDebugInfo.FormattingEnabled = True
            Me.cboDebugInfo.Items.AddRange(New Object() {"none", "full", "pdb-only"})
            Me.cboDebugInfo.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboDebugInfo.Name = "cboDebugInfo"
            '
            'lblFileAlignment
            '
            resources.ApplyResources(Me.lblFileAlignment, "lblFileAlignment")
            Me.lblFileAlignment.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.lblFileAlignment.Name = "lblFileAlignment"
            '
            'cboFileAlignment
            '
            resources.ApplyResources(Me.cboFileAlignment, "cboFileAlignment")
            Me.cboFileAlignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboFileAlignment.FormattingEnabled = True
            Me.cboFileAlignment.Items.AddRange(New Object() {"512", "1024", "2048", "4096", "8192"})
            Me.cboFileAlignment.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.cboFileAlignment.Name = "cboFileAlignment"
            '
            'lblDLLBase
            '
            resources.ApplyResources(Me.lblDLLBase, "lblDLLBase")
            Me.lblDLLBase.Margin = New System.Windows.Forms.Padding(9, 3, 3, 0)
            Me.lblDLLBase.Name = "lblDLLBase"
            '
            'txtDLLBase
            '
            resources.ApplyResources(Me.txtDLLBase, "txtDLLBase")
            Me.txtDLLBase.Margin = New System.Windows.Forms.Padding(3, 3, 0, 0)
            Me.txtDLLBase.Name = "txtDLLBase"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overarchingTableLayoutPanel.Controls.Add(Me.outputTableLayoutPanel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtDLLBase, 1, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.generalTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblDLLBase, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblFileAlignment, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboDebugInfo, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboFileAlignment, 1, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblDebugInfo, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboReportCompilerErrors, 1, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboLanguageVersion, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkOverflow, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblReportCompilerErrors, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblLanguageVersion, 0, 1)
            Me.overarchingTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'outputTableLayoutPanel
            '
            resources.ApplyResources(Me.outputTableLayoutPanel, "outputTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.outputTableLayoutPanel, 2)
            Me.outputTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.outputTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLabel)
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLineLabel)
            Me.outputTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 3, 0, 3)
            Me.outputTableLayoutPanel.Name = "outputTableLayoutPanel"
            Me.outputTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'outputLabel
            '
            resources.ApplyResources(Me.outputLabel, "outputLabel")
            Me.outputLabel.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.outputLabel.Name = "outputLabel"
            '
            'outputLineLabel
            '
            resources.ApplyResources(Me.outputLineLabel, "outputLineLabel")
            Me.outputLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.outputLineLabel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.outputLineLabel.Name = "outputLineLabel"
            '
            'generalTableLayoutPanel
            '
            resources.ApplyResources(Me.generalTableLayoutPanel, "generalTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.generalTableLayoutPanel, 2)
            Me.generalTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.generalTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.generalTableLayoutPanel.Controls.Add(Me.generalLabel, 0, 0)
            Me.generalTableLayoutPanel.Controls.Add(Me.generalLineLabel, 1, 0)
            Me.generalTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 0, 0, 3)
            Me.generalTableLayoutPanel.Name = "generalTableLayoutPanel"
            Me.generalTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'generalLabel
            '
            resources.ApplyResources(Me.generalLabel, "generalLabel")
            Me.generalLabel.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.generalLabel.Name = "generalLabel"
            '
            'generalLineLabel
            '
            resources.ApplyResources(Me.generalLineLabel, "generalLineLabel")
            Me.generalLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.generalLineLabel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.generalLineLabel.Name = "generalLineLabel"
            '
            'chkOverflow
            '
            resources.ApplyResources(Me.chkOverflow, "chkOverflow")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkOverflow, 2)
            Me.chkOverflow.Margin = New System.Windows.Forms.Padding(9, 3, 3, 3)
            Me.chkOverflow.Name = "chkOverflow"
            '
            'AdvBuildSettingsPropPage
            '
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AdvBuildSettingsPropPage"
            resources.ApplyResources(Me, "$this")
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.outputTableLayoutPanel.ResumeLayout(False)
            Me.outputTableLayoutPanel.PerformLayout()
            Me.generalTableLayoutPanel.ResumeLayout(False)
            Me.generalTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub
    End Class

End Namespace

