' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class CodeAnalysisPropPage
        Friend WithEvents FxCopAnalyzersPanel As System.Windows.Forms.Panel
        Friend WithEvents InstalledVersionLabel As System.Windows.Forms.Label
        Friend WithEvents InstalledVersionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents InstallCustomVersionButton As System.Windows.Forms.Button
        Friend WithEvents InstallLatestVersionButton As System.Windows.Forms.Button
        Friend WithEvents UninstallAnalyzersButton As System.Windows.Forms.Button
        Friend WithEvents FxCopAnalyzersPackageNameTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FxCopAnalyzersPackageNameLabel As System.Windows.Forms.Label
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
            Me.FxCopAnalyzersPackageNameTextBox = New System.Windows.Forms.TextBox()
            Me.FxCopAnalyzersPackageNameLabel = New System.Windows.Forms.Label()
            Me.InstalledVersionTextBox = New System.Windows.Forms.TextBox()
            Me.InstalledVersionLabel = New System.Windows.Forms.Label()
            Me.InstallLatestVersionButton = New System.Windows.Forms.Button()
            Me.InstallCustomVersionButton = New System.Windows.Forms.Button()
            Me.UninstallAnalyzersButton = New System.Windows.Forms.Button()
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
            Me.FxCopAnalyzersPanel.Controls.Add(Me.FxCopAnalyzersPackageNameTextBox)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.FxCopAnalyzersPackageNameLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.InstalledVersionTextBox)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.InstalledVersionLabel)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.InstallLatestVersionButton)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.InstallCustomVersionButton)
            Me.FxCopAnalyzersPanel.Controls.Add(Me.UninstallAnalyzersButton)
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
            'FxCopAnalyzersPackageNameTextBox
            '
            resources.ApplyResources(Me.FxCopAnalyzersPackageNameTextBox, "FxCopAnalyzersPackageNameTextBox")
            Me.FxCopAnalyzersPackageNameTextBox.Name = "FxCopAnalyzersPackageNameTextBox"
            Me.FxCopAnalyzersPackageNameTextBox.ReadOnly = True
            '
            'FxCopAnalyzersPackageNameLabel
            '
            resources.ApplyResources(Me.FxCopAnalyzersPackageNameLabel, "FxCopAnalyzersPackageNameLabel")
            Me.FxCopAnalyzersPackageNameLabel.Name = "FxCopAnalyzersPackageNameLabel"
            '
            'InstalledVersionTextBox
            '
            resources.ApplyResources(Me.InstalledVersionTextBox, "InstalledVersionTextBox")
            Me.InstalledVersionTextBox.Name = "InstalledVersionTextBox"
            Me.InstalledVersionTextBox.ReadOnly = True
            '
            'InstalledVersionLabel
            '
            resources.ApplyResources(Me.InstalledVersionLabel, "InstalledVersionLabel")
            Me.InstalledVersionLabel.Name = "InstalledVersionLabel"
            '
            'InstallLatestVersionButton
            '
            resources.ApplyResources(Me.InstallLatestVersionButton, "InstallLatestVersionButton")
            Me.InstallLatestVersionButton.Name = "InstallLatestVersionButton"
            Me.InstallLatestVersionButton.UseVisualStyleBackColor = True
            '
            'InstallCustomVersionButton
            '
            resources.ApplyResources(Me.InstallCustomVersionButton, "InstallCustomVersionButton")
            Me.InstallCustomVersionButton.Name = "InstallCustomVersionButton"
            Me.InstallCustomVersionButton.UseVisualStyleBackColor = True
            '
            'UninstallAnalyzersButton
            '
            resources.ApplyResources(Me.UninstallAnalyzersButton, "UninstallAnalyzersButton")
            Me.UninstallAnalyzersButton.Name = "UninstallAnalyzersButton"
            Me.UninstallAnalyzersButton.UseVisualStyleBackColor = True
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
