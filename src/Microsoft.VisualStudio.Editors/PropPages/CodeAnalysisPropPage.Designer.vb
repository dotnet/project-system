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
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RunAnalyzersDuringLiveAnalysis)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RunAnalyzersDuringBuild)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RoslynAnalyzersLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RoslynAnalyzersLineLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.RoslynAnalyzersHelpLinkLabel)
            resources.ApplyResources(Me.FxCopAnalyzersPanel, "FxCopAnalyzersPanel")
            Me.FxCopAnalyzersPanel.Name = "FxCopAnalyzersPanel"
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
    End Class

End Namespace
