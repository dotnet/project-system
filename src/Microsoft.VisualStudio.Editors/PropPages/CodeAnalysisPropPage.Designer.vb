' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class CodeAnalysisPropPage
        Friend WithEvents FxCopAnalyzersPanel As System.Windows.Forms.Panel
        Friend WithEvents RoslynAnalyzersLabel As System.Windows.Forms.Label
        Friend WithEvents RoslynAnalyzersLineLabel As System.Windows.Forms.Label
        Friend WithEvents RoslynAnalyzersHelpLinkLabel As System.Windows.Forms.LinkLabel
        Friend WithEvents RunAnalyzersDuringLiveAnalysis As System.Windows.Forms.CheckBox
        Friend WithEvents RunAnalyzersDuringBuild As System.Windows.Forms.CheckBox
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(CodeAnalysisPropPage))
            Me.FxCopAnalyzersPanel = New System.Windows.Forms.Panel()
            Me.EnforceCodeStyleInBuildCheckBox = New System.Windows.Forms.CheckBox()
            Me.NETAnalyzersLinkLabel = New System.Windows.Forms.LinkLabel()
            Me.EnableNETAnalyzersCheckBox = New System.Windows.Forms.CheckBox()
            Me.Label1 = New System.Windows.Forms.Label()
            Me.Label2 = New System.Windows.Forms.Label()
            Me.AnalysisLevelLabel = New System.Windows.Forms.Label()
            Me.AnalysisLevelComboBox = New System.Windows.Forms.ComboBox()
            Me.RunAnalyzersDuringLiveAnalysis = New System.Windows.Forms.CheckBox()
            Me.RunAnalyzersDuringBuild = New System.Windows.Forms.CheckBox()
            Me.RoslynAnalyzersLabel = New System.Windows.Forms.Label()
            Me.RoslynAnalyzersLineLabel = New System.Windows.Forms.Label()
            Me.RoslynAnalyzersHelpLinkLabel = New System.Windows.Forms.LinkLabel()
            Me.FxCopAnalyzersPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'FxCopAnalyzersPanel
            '
            Me.FxCopAnalyzersPanel.Controls.Add(Me.EnforceCodeStyleInBuildCheckBox)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.NETAnalyzersLinkLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.EnableNETAnalyzersCheckBox)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.Label1)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.Label2)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.AnalysisLevelLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.AnalysisLevelComboBox)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RunAnalyzersDuringLiveAnalysis)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RunAnalyzersDuringBuild)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RoslynAnalyzersLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RoslynAnalyzersLineLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RoslynAnalyzersHelpLinkLabel)
            resources.ApplyResources(Me.FxCopAnalyzersPanel, "FxCopAnalyzersPanel")
            Me.FxCopAnalyzersPanel.Name = "FxCopAnalyzersPanel"
            '
            'EnforceCodeStyleInBuildCheckBox
            '
            resources.ApplyResources(Me.EnforceCodeStyleInBuildCheckBox, "EnforceCodeStyleInBuildCheckBox")
            Me.EnforceCodeStyleInBuildCheckBox.Checked = True
            Me.EnforceCodeStyleInBuildCheckBox.CheckState = System.Windows.Forms.CheckState.Checked
            Me.EnforceCodeStyleInBuildCheckBox.Name = "EnforceCodeStyleInBuildCheckBox"
            Me.EnforceCodeStyleInBuildCheckBox.UseVisualStyleBackColor = True
            '
            'NETAnalyzersLinkLabel
            '
            resources.ApplyResources(Me.NETAnalyzersLinkLabel, "NETAnalyzersLinkLabel")
            Me.NETAnalyzersLinkLabel.Name = "NETAnalyzersLinkLabel"
            Me.NETAnalyzersLinkLabel.TabStop = True
            '
            'EnableNETAnalyzersCheckBox
            '
            resources.ApplyResources(Me.EnableNETAnalyzersCheckBox, "EnableNETAnalyzersCheckBox")
            Me.EnableNETAnalyzersCheckBox.Checked = True
            Me.EnableNETAnalyzersCheckBox.CheckState = System.Windows.Forms.CheckState.Checked
            Me.EnableNETAnalyzersCheckBox.Name = "EnableNETAnalyzersCheckBox"
            Me.EnableNETAnalyzersCheckBox.UseVisualStyleBackColor = True
            '
            'Label1
            '
            resources.ApplyResources(Me.Label1, "Label1")
            Me.Label1.Name = "Label1"
            '
            'Label2
            '
            Me.Label2.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            Me.Label2.BackColor = System.Drawing.SystemColors.ControlDark
            resources.ApplyResources(Me.Label2, "Label2")
            Me.Label2.Name = "Label2"
            '
            'AnalysisLevelLabel
            '
            resources.ApplyResources(Me.AnalysisLevelLabel, "AnalysisLevelLabel")
            Me.AnalysisLevelLabel.Name = "AnalysisLevelLabel"
            '
            'AnalysisLevelComboBox
            '
            Me.AnalysisLevelComboBox.AutoCompleteCustomSource.AddRange(New String() {resources.GetString("AnalysisLevelComboBox.AutoCompleteCustomSource"), resources.GetString("AnalysisLevelComboBox.AutoCompleteCustomSource1"), resources.GetString("AnalysisLevelComboBox.AutoCompleteCustomSource2"), resources.GetString("AnalysisLevelComboBox.AutoCompleteCustomSource3")})
            Me.AnalysisLevelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.AnalysisLevelComboBox.FormattingEnabled = True
            resources.ApplyResources(Me.AnalysisLevelComboBox, "AnalysisLevelComboBox")
            Me.AnalysisLevelComboBox.Name = "AnalysisLevelComboBox"
            Me.AnalysisLevelComboBox.AccessibleName = Me.AnalysisLevelLabel.Text
            '
            'RunAnalyzersDuringLiveAnalysis
            '
            resources.ApplyResources(Me.RunAnalyzersDuringLiveAnalysis, "RunAnalyzersDuringLiveAnalysis")
            Me.RunAnalyzersDuringLiveAnalysis.Checked = True
            Me.RunAnalyzersDuringLiveAnalysis.CheckState = System.Windows.Forms.CheckState.Checked
            Me.RunAnalyzersDuringLiveAnalysis.Name = "RunAnalyzersDuringLiveAnalysis"
            Me.RunAnalyzersDuringLiveAnalysis.UseVisualStyleBackColor = True
            '
            'RunAnalyzersDuringBuild
            '
            resources.ApplyResources(Me.RunAnalyzersDuringBuild, "RunAnalyzersDuringBuild")
            Me.RunAnalyzersDuringBuild.Checked = True
            Me.RunAnalyzersDuringBuild.CheckState = System.Windows.Forms.CheckState.Checked
            Me.RunAnalyzersDuringBuild.Name = "RunAnalyzersDuringBuild"
            Me.RunAnalyzersDuringBuild.UseVisualStyleBackColor = True
            '
            'RoslynAnalyzersLabel
            '
            resources.ApplyResources(Me.RoslynAnalyzersLabel, "RoslynAnalyzersLabel")
            Me.RoslynAnalyzersLabel.Name = "RoslynAnalyzersLabel"
            '
            'RoslynAnalyzersLineLabel
            '
            Me.RoslynAnalyzersLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            Me.RoslynAnalyzersLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            resources.ApplyResources(Me.RoslynAnalyzersLineLabel, "RoslynAnalyzersLineLabel")
            Me.RoslynAnalyzersLineLabel.Name = "RoslynAnalyzersLineLabel"
            '
            'RoslynAnalyzersHelpLinkLabel
            '
            resources.ApplyResources(Me.RoslynAnalyzersHelpLinkLabel, "RoslynAnalyzersHelpLinkLabel")
            Me.RoslynAnalyzersHelpLinkLabel.Name = "RoslynAnalyzersHelpLinkLabel"
            Me.RoslynAnalyzersHelpLinkLabel.TabStop = True
            '
            'CodeAnalysisPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.FxCopAnalyzersPanel)
            Me.Name = "CodeAnalysisPropPage"
            Me.FxCopAnalyzersPanel.ResumeLayout(False)
            Me.FxCopAnalyzersPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

        Friend WithEvents AnalysisLevelComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents AnalysisLevelLabel As System.Windows.Forms.Label
        Friend WithEvents EnableNETAnalyzersCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents Label1 As System.Windows.Forms.Label
        Friend WithEvents Label2 As System.Windows.Forms.Label
        Friend WithEvents NETAnalyzersLinkLabel As System.Windows.Forms.LinkLabel
        Friend WithEvents EnforceCodeStyleInBuildCheckBox As System.Windows.Forms.CheckBox
    End Class

End Namespace
