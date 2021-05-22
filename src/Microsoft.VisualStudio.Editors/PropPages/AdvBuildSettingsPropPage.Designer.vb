' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvBuildSettingsPropPage))
            Me.lblLanguageVersion = New System.Windows.Forms.Label()
            Me.lblReportCompilerErrors = New System.Windows.Forms.Label()
            Me.cboLanguageVersion = New System.Windows.Forms.ComboBox()
            Me.cboReportCompilerErrors = New System.Windows.Forms.ComboBox()
            Me.lblDebugInfo = New System.Windows.Forms.Label()
            Me.cboDebugInfo = New System.Windows.Forms.ComboBox()
            Me.lblFileAlignment = New System.Windows.Forms.Label()
            Me.cboFileAlignment = New System.Windows.Forms.ComboBox()
            Me.lblDLLBase = New System.Windows.Forms.Label()
            Me.txtDLLBase = New System.Windows.Forms.TextBox()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.outputTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.outputLabel = New System.Windows.Forms.Label()
            Me.outputLineLabel = New System.Windows.Forms.Label()
            Me.generalTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.generalLabel = New System.Windows.Forms.Label()
            Me.generalLineLabel = New System.Windows.Forms.Label()
            Me.chkOverflow = New System.Windows.Forms.CheckBox()
            Me.lnkLabel = New System.Windows.Forms.LinkLabel()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.outputTableLayoutPanel.SuspendLayout()
            Me.generalTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'lblLanguageVersion
            '
            resources.ApplyResources(Me.lblLanguageVersion, "lblLanguageVersion")
            Me.lblLanguageVersion.Name = "lblLanguageVersion"
            '
            'lblReportCompilerErrors
            '
            resources.ApplyResources(Me.lblReportCompilerErrors, "lblReportCompilerErrors")
            Me.lblReportCompilerErrors.Name = "lblReportCompilerErrors"
            '
            'cboLanguageVersion
            '
            resources.ApplyResources(Me.cboLanguageVersion, "cboLanguageVersion")
            Me.cboLanguageVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboLanguageVersion.FormattingEnabled = True
            Me.cboLanguageVersion.Name = "cboLanguageVersion"
            '
            'cboReportCompilerErrors
            '
            resources.ApplyResources(Me.cboReportCompilerErrors, "cboReportCompilerErrors")
            Me.cboReportCompilerErrors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboReportCompilerErrors.FormattingEnabled = True
            Me.cboReportCompilerErrors.Name = "cboReportCompilerErrors"
            '
            'lblDebugInfo
            '
            resources.ApplyResources(Me.lblDebugInfo, "lblDebugInfo")
            Me.lblDebugInfo.Name = "lblDebugInfo"
            '
            'cboDebugInfo
            '
            resources.ApplyResources(Me.cboDebugInfo, "cboDebugInfo")
            Me.cboDebugInfo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboDebugInfo.FormattingEnabled = True
            Me.cboDebugInfo.Name = "cboDebugInfo"
            '
            'lblFileAlignment
            '
            resources.ApplyResources(Me.lblFileAlignment, "lblFileAlignment")
            Me.lblFileAlignment.Name = "lblFileAlignment"
            '
            'cboFileAlignment
            '
            resources.ApplyResources(Me.cboFileAlignment, "cboFileAlignment")
            Me.cboFileAlignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboFileAlignment.FormattingEnabled = True
            Me.cboFileAlignment.Items.AddRange(New Object() {resources.GetString("cboFileAlignment.Items"), resources.GetString("cboFileAlignment.Items1"), resources.GetString("cboFileAlignment.Items2"), resources.GetString("cboFileAlignment.Items3"), resources.GetString("cboFileAlignment.Items4")})
            Me.cboFileAlignment.Name = "cboFileAlignment"
            '
            'lblDLLBase
            '
            resources.ApplyResources(Me.lblDLLBase, "lblDLLBase")
            Me.lblDLLBase.Name = "lblDLLBase"
            '
            'txtDLLBase
            '
            resources.ApplyResources(Me.txtDLLBase, "txtDLLBase")
            Me.txtDLLBase.Name = "txtDLLBase"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.outputTableLayoutPanel, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtDLLBase, 1, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.generalTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblDLLBase, 0, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblFileAlignment, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboDebugInfo, 1, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboFileAlignment, 1, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblDebugInfo, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboReportCompilerErrors, 1, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboLanguageVersion, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkOverflow, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblReportCompilerErrors, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblLanguageVersion, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lnkLabel, 1, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'outputTableLayoutPanel
            '
            resources.ApplyResources(Me.outputTableLayoutPanel, "outputTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.outputTableLayoutPanel, 2)
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLabel)
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLineLabel)
            Me.outputTableLayoutPanel.Name = "outputTableLayoutPanel"
            '
            'outputLabel
            '
            resources.ApplyResources(Me.outputLabel, "outputLabel")
            Me.outputLabel.Name = "outputLabel"
            '
            'outputLineLabel
            '
            resources.ApplyResources(Me.outputLineLabel, "outputLineLabel")
            Me.outputLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.outputLineLabel.Name = "outputLineLabel"
            '
            'generalTableLayoutPanel
            '
            resources.ApplyResources(Me.generalTableLayoutPanel, "generalTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.generalTableLayoutPanel, 2)
            Me.generalTableLayoutPanel.Controls.Add(Me.generalLabel, 0, 0)
            Me.generalTableLayoutPanel.Controls.Add(Me.generalLineLabel, 1, 0)
            Me.generalTableLayoutPanel.Name = "generalTableLayoutPanel"
            '
            'generalLabel
            '
            resources.ApplyResources(Me.generalLabel, "generalLabel")
            Me.generalLabel.Name = "generalLabel"
            '
            'generalLineLabel
            '
            resources.ApplyResources(Me.generalLineLabel, "generalLineLabel")
            Me.generalLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.generalLineLabel.Name = "generalLineLabel"
            '
            'chkOverflow
            '
            resources.ApplyResources(Me.chkOverflow, "chkOverflow")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkOverflow, 2)
            Me.chkOverflow.Name = "chkOverflow"
            '
            'lnkLabel
            '
            resources.ApplyResources(Me.lnkLabel, "lnkLabel")
            Me.lnkLabel.Name = "lnkLabel"
            Me.lnkLabel.TabStop = True
            '
            'AdvBuildSettingsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AdvBuildSettingsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.outputTableLayoutPanel.ResumeLayout(False)
            Me.outputTableLayoutPanel.PerformLayout()
            Me.generalTableLayoutPanel.ResumeLayout(False)
            Me.generalTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Friend WithEvents lnkLabel As System.Windows.Forms.LinkLabel
    End Class

End Namespace

