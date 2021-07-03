' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Class BuildPropPage

        Friend WithEvents lblConditionalCompilationSymbols As System.Windows.Forms.Label
        Friend WithEvents lblPlatformTarget As System.Windows.Forms.Label
        Friend WithEvents txtConditionalCompilationSymbols As System.Windows.Forms.TextBox
        Friend WithEvents chkDefineDebug As System.Windows.Forms.CheckBox
        Friend WithEvents chkDefineTrace As System.Windows.Forms.CheckBox
        Friend WithEvents cboPlatformTarget As System.Windows.Forms.ComboBox
        Friend WithEvents chkPrefer32Bit As System.Windows.Forms.CheckBox
        Friend WithEvents chkAllowUnsafeCode As System.Windows.Forms.CheckBox
        Friend WithEvents chkOptimizeCode As System.Windows.Forms.CheckBox
        Friend WithEvents lblWarningLevel As System.Windows.Forms.Label
        Friend WithEvents lblSupressWarnings As System.Windows.Forms.Label
        Friend WithEvents cboWarningLevel As System.Windows.Forms.ComboBox
        Friend WithEvents txtSupressWarnings As System.Windows.Forms.TextBox
        Friend WithEvents rbWarningNone As System.Windows.Forms.RadioButton
        Friend WithEvents rbWarningSpecific As System.Windows.Forms.RadioButton
        Friend WithEvents rbWarningAll As System.Windows.Forms.RadioButton
        Friend WithEvents txtSpecificWarnings As System.Windows.Forms.TextBox
        Friend WithEvents lblOutputPath As System.Windows.Forms.Label
        Friend WithEvents txtOutputPath As System.Windows.Forms.TextBox
        Friend WithEvents btnOutputPathBrowse As System.Windows.Forms.Button
        Friend WithEvents chkXMLDocumentationFile As System.Windows.Forms.CheckBox
        Friend WithEvents chkRegisterForCOM As System.Windows.Forms.CheckBox
        Friend WithEvents txtXMLDocumentationFile As System.Windows.Forms.TextBox
        Friend WithEvents btnAdvanced As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalHeaderTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalLabel As System.Windows.Forms.Label
        Friend WithEvents generalLineLabel As System.Windows.Forms.Label
        Friend WithEvents errorsAndWarningsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents errorsAndWarningsLineLabel As System.Windows.Forms.Label
        Friend WithEvents errorsAndWarningsLabel As System.Windows.Forms.Label
        Friend WithEvents treatWarningsAsErrorsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents treatWarningsAsErrorsLineLabel As System.Windows.Forms.Label
        Friend WithEvents treatWarningsAsErrorsLabel As System.Windows.Forms.Label
        Friend WithEvents outputTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents outputLineLabel As System.Windows.Forms.Label
        Friend WithEvents outputLabel As System.Windows.Forms.Label
        Friend WithEvents lblSGenOption As System.Windows.Forms.Label
        Friend WithEvents cboSGenOption As System.Windows.Forms.ComboBox
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(BuildPropPage))
            Me.lblConditionalCompilationSymbols = New System.Windows.Forms.Label()
            Me.txtConditionalCompilationSymbols = New System.Windows.Forms.TextBox()
            Me.chkDefineDebug = New System.Windows.Forms.CheckBox()
            Me.chkDefineTrace = New System.Windows.Forms.CheckBox()
            Me.lblPlatformTarget = New System.Windows.Forms.Label()
            Me.cboPlatformTarget = New System.Windows.Forms.ComboBox()
            Me.chkPrefer32Bit = New System.Windows.Forms.CheckBox()
            Me.chkAllowUnsafeCode = New System.Windows.Forms.CheckBox()
            Me.chkOptimizeCode = New System.Windows.Forms.CheckBox()
            Me.lblWarningLevel = New System.Windows.Forms.Label()
            Me.cboWarningLevel = New System.Windows.Forms.ComboBox()
            Me.lblSupressWarnings = New System.Windows.Forms.Label()
            Me.txtSupressWarnings = New System.Windows.Forms.TextBox()
            Me.rbWarningNone = New System.Windows.Forms.RadioButton()
            Me.rbWarningSpecific = New System.Windows.Forms.RadioButton()
            Me.rbWarningAll = New System.Windows.Forms.RadioButton()
            Me.txtSpecificWarnings = New System.Windows.Forms.TextBox()
            Me.lblOutputPath = New System.Windows.Forms.Label()
            Me.txtOutputPath = New System.Windows.Forms.TextBox()
            Me.btnOutputPathBrowse = New System.Windows.Forms.Button()
            Me.chkXMLDocumentationFile = New System.Windows.Forms.CheckBox()
            Me.chkRegisterForCOM = New System.Windows.Forms.CheckBox()
            Me.txtXMLDocumentationFile = New System.Windows.Forms.TextBox()
            Me.btnAdvanced = New System.Windows.Forms.Button()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.outputTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.outputLineLabel = New System.Windows.Forms.Label()
            Me.outputLabel = New System.Windows.Forms.Label()
            Me.generalHeaderTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.generalLineLabel = New System.Windows.Forms.Label()
            Me.generalLabel = New System.Windows.Forms.Label()
            Me.treatWarningsAsErrorsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.treatWarningsAsErrorsLineLabel = New System.Windows.Forms.Label()
            Me.treatWarningsAsErrorsLabel = New System.Windows.Forms.Label()
            Me.errorsAndWarningsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.errorsAndWarningsLineLabel = New System.Windows.Forms.Label()
            Me.errorsAndWarningsLabel = New System.Windows.Forms.Label()
            Me.lblSGenOption = New System.Windows.Forms.Label()
            Me.cboSGenOption = New System.Windows.Forms.ComboBox()
            Me.lblNullable = New System.Windows.Forms.Label()
            Me.cboNullable = New System.Windows.Forms.ComboBox()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.outputTableLayoutPanel.SuspendLayout()
            Me.generalHeaderTableLayoutPanel.SuspendLayout()
            Me.treatWarningsAsErrorsTableLayoutPanel.SuspendLayout()
            Me.errorsAndWarningsTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'lblConditionalCompilationSymbols
            '
            resources.ApplyResources(Me.lblConditionalCompilationSymbols, "lblConditionalCompilationSymbols")
            Me.lblConditionalCompilationSymbols.Name = "lblConditionalCompilationSymbols"
            '
            'txtConditionalCompilationSymbols
            '
            resources.ApplyResources(Me.txtConditionalCompilationSymbols, "txtConditionalCompilationSymbols")
            Me.txtConditionalCompilationSymbols.Name = "txtConditionalCompilationSymbols"
            '
            'chkDefineDebug
            '
            resources.ApplyResources(Me.chkDefineDebug, "chkDefineDebug")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkDefineDebug, 3)
            Me.chkDefineDebug.Name = "chkDefineDebug"
            '
            'chkDefineTrace
            '
            resources.ApplyResources(Me.chkDefineTrace, "chkDefineTrace")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkDefineTrace, 3)
            Me.chkDefineTrace.Name = "chkDefineTrace"
            '
            'lblPlatformTarget
            '
            resources.ApplyResources(Me.lblPlatformTarget, "lblPlatformTarget")
            Me.lblPlatformTarget.Name = "lblPlatformTarget"
            '
            'cboPlatformTarget
            '
            resources.ApplyResources(Me.cboPlatformTarget, "cboPlatformTarget")
            Me.cboPlatformTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboPlatformTarget.FormattingEnabled = True
            Me.cboPlatformTarget.Name = "cboPlatformTarget"
            '
            'chkPrefer32Bit
            '
            resources.ApplyResources(Me.chkPrefer32Bit, "chkPrefer32Bit")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkPrefer32Bit, 3)
            Me.chkPrefer32Bit.Name = "chkPrefer32Bit"
            '
            'chkAllowUnsafeCode
            '
            resources.ApplyResources(Me.chkAllowUnsafeCode, "chkAllowUnsafeCode")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkAllowUnsafeCode, 3)
            Me.chkAllowUnsafeCode.Name = "chkAllowUnsafeCode"
            '
            'chkOptimizeCode
            '
            resources.ApplyResources(Me.chkOptimizeCode, "chkOptimizeCode")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkOptimizeCode, 3)
            Me.chkOptimizeCode.Name = "chkOptimizeCode"
            '
            'lblWarningLevel
            '
            resources.ApplyResources(Me.lblWarningLevel, "lblWarningLevel")
            Me.lblWarningLevel.Name = "lblWarningLevel"
            '
            'cboWarningLevel
            '
            resources.ApplyResources(Me.cboWarningLevel, "cboWarningLevel")
            Me.cboWarningLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboWarningLevel.FormattingEnabled = True
            Me.cboWarningLevel.Items.AddRange(New Object() {resources.GetString("cboWarningLevel.Items"), resources.GetString("cboWarningLevel.Items1"), resources.GetString("cboWarningLevel.Items2"), resources.GetString("cboWarningLevel.Items3"), resources.GetString("cboWarningLevel.Items4"), resources.GetString("cboWarningLevel.Items5")})
            Me.cboWarningLevel.Name = "cboWarningLevel"
            '
            'lblSupressWarnings
            '
            resources.ApplyResources(Me.lblSupressWarnings, "lblSupressWarnings")
            Me.lblSupressWarnings.Name = "lblSupressWarnings"
            '
            'txtSupressWarnings
            '
            resources.ApplyResources(Me.txtSupressWarnings, "txtSupressWarnings")
            Me.txtSupressWarnings.Name = "txtSupressWarnings"
            '
            'rbWarningNone
            '
            resources.ApplyResources(Me.rbWarningNone, "rbWarningNone")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.rbWarningNone, 3)
            Me.rbWarningNone.Name = "rbWarningNone"
            '
            'rbWarningSpecific
            '
            resources.ApplyResources(Me.rbWarningSpecific, "rbWarningSpecific")
            Me.rbWarningSpecific.Name = "rbWarningSpecific"
            '
            'rbWarningAll
            '
            resources.ApplyResources(Me.rbWarningAll, "rbWarningAll")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.rbWarningAll, 3)
            Me.rbWarningAll.Name = "rbWarningAll"
            '
            'txtSpecificWarnings
            '
            resources.ApplyResources(Me.txtSpecificWarnings, "txtSpecificWarnings")
            Me.txtSpecificWarnings.Name = "txtSpecificWarnings"
            '
            'lblOutputPath
            '
            resources.ApplyResources(Me.lblOutputPath, "lblOutputPath")
            Me.lblOutputPath.Name = "lblOutputPath"
            '
            'txtOutputPath
            '
            resources.ApplyResources(Me.txtOutputPath, "txtOutputPath")
            Me.txtOutputPath.Name = "txtOutputPath"
            '
            'btnOutputPathBrowse
            '
            resources.ApplyResources(Me.btnOutputPathBrowse, "btnOutputPathBrowse")
            Me.btnOutputPathBrowse.Name = "btnOutputPathBrowse"
            '
            'chkXMLDocumentationFile
            '
            resources.ApplyResources(Me.chkXMLDocumentationFile, "chkXMLDocumentationFile")
            Me.chkXMLDocumentationFile.Name = "chkXMLDocumentationFile"
            '
            'chkRegisterForCOM
            '
            resources.ApplyResources(Me.chkRegisterForCOM, "chkRegisterForCOM")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.chkRegisterForCOM, 3)
            Me.chkRegisterForCOM.Name = "chkRegisterForCOM"
            '
            'txtXMLDocumentationFile
            '
            resources.ApplyResources(Me.txtXMLDocumentationFile, "txtXMLDocumentationFile")
            Me.txtXMLDocumentationFile.Name = "txtXMLDocumentationFile"
            '
            'btnAdvanced
            '
            resources.ApplyResources(Me.btnAdvanced, "btnAdvanced")
            Me.btnAdvanced.Name = "btnAdvanced"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.outputTableLayoutPanel, 0, 16)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.generalHeaderTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.treatWarningsAsErrorsTableLayoutPanel, 0, 12)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.errorsAndWarningsTableLayoutPanel, 0, 9)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkRegisterForCOM, 0, 19)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkXMLDocumentationFile, 0, 18)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtXMLDocumentationFile, 1, 18)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.btnOutputPathBrowse, 2, 17)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtOutputPath, 1, 17)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblOutputPath, 0, 17)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.rbWarningSpecific, 0, 15)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtSpecificWarnings, 1, 15)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.rbWarningAll, 0, 14)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.rbWarningNone, 0, 13)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtSupressWarnings, 1, 11)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblSupressWarnings, 0, 11)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboWarningLevel, 1, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblWarningLevel, 0, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboPlatformTarget, 1, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkOptimizeCode, 0, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkAllowUnsafeCode, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkPrefer32Bit, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkDefineTrace, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.chkDefineDebug, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtConditionalCompilationSymbols, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblConditionalCompilationSymbols, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblPlatformTarget, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblSGenOption, 0, 20)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboSGenOption, 1, 20)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.btnAdvanced, 2, 21)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblNullable, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.cboNullable, 1, 5)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'outputTableLayoutPanel
            '
            resources.ApplyResources(Me.outputTableLayoutPanel, "outputTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.outputTableLayoutPanel, 3)
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLineLabel, 1, 0)
            Me.outputTableLayoutPanel.Controls.Add(Me.outputLabel, 0, 0)
            Me.outputTableLayoutPanel.Name = "outputTableLayoutPanel"
            '
            'outputLineLabel
            '
            Me.outputLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.outputLineLabel, "outputLineLabel")
            Me.outputLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.outputLineLabel.Name = "outputLineLabel"
            '
            'outputLabel
            '
            resources.ApplyResources(Me.outputLabel, "outputLabel")
            Me.outputLabel.Name = "outputLabel"
            '
            'generalHeaderTableLayoutPanel
            '
            resources.ApplyResources(Me.generalHeaderTableLayoutPanel, "generalHeaderTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.generalHeaderTableLayoutPanel, 3)
            Me.generalHeaderTableLayoutPanel.Controls.Add(Me.generalLineLabel, 1, 0)
            Me.generalHeaderTableLayoutPanel.Controls.Add(Me.generalLabel, 0, 0)
            Me.generalHeaderTableLayoutPanel.Name = "generalHeaderTableLayoutPanel"
            '
            'generalLineLabel
            '
            Me.generalLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.generalLineLabel, "generalLineLabel")
            Me.generalLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.generalLineLabel.Name = "generalLineLabel"
            '
            'generalLabel
            '
            resources.ApplyResources(Me.generalLabel, "generalLabel")
            Me.generalLabel.Name = "generalLabel"
            '
            'treatWarningsAsErrorsTableLayoutPanel
            '
            resources.ApplyResources(Me.treatWarningsAsErrorsTableLayoutPanel, "treatWarningsAsErrorsTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.treatWarningsAsErrorsTableLayoutPanel, 3)
            Me.treatWarningsAsErrorsTableLayoutPanel.Controls.Add(Me.treatWarningsAsErrorsLineLabel, 1, 0)
            Me.treatWarningsAsErrorsTableLayoutPanel.Controls.Add(Me.treatWarningsAsErrorsLabel, 0, 0)
            Me.treatWarningsAsErrorsTableLayoutPanel.Name = "treatWarningsAsErrorsTableLayoutPanel"
            '
            'treatWarningsAsErrorsLineLabel
            '
            Me.treatWarningsAsErrorsLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.treatWarningsAsErrorsLineLabel, "treatWarningsAsErrorsLineLabel")
            Me.treatWarningsAsErrorsLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.treatWarningsAsErrorsLineLabel.Name = "treatWarningsAsErrorsLineLabel"
            '
            'treatWarningsAsErrorsLabel
            '
            resources.ApplyResources(Me.treatWarningsAsErrorsLabel, "treatWarningsAsErrorsLabel")
            Me.treatWarningsAsErrorsLabel.Name = "treatWarningsAsErrorsLabel"
            '
            'errorsAndWarningsTableLayoutPanel
            '
            resources.ApplyResources(Me.errorsAndWarningsTableLayoutPanel, "errorsAndWarningsTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.errorsAndWarningsTableLayoutPanel, 3)
            Me.errorsAndWarningsTableLayoutPanel.Controls.Add(Me.errorsAndWarningsLineLabel, 1, 0)
            Me.errorsAndWarningsTableLayoutPanel.Controls.Add(Me.errorsAndWarningsLabel, 0, 0)
            Me.errorsAndWarningsTableLayoutPanel.Name = "errorsAndWarningsTableLayoutPanel"
            '
            'errorsAndWarningsLineLabel
            '
            Me.errorsAndWarningsLineLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.errorsAndWarningsLineLabel, "errorsAndWarningsLineLabel")
            Me.errorsAndWarningsLineLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.errorsAndWarningsLineLabel.Name = "errorsAndWarningsLineLabel"
            '
            'errorsAndWarningsLabel
            '
            resources.ApplyResources(Me.errorsAndWarningsLabel, "errorsAndWarningsLabel")
            Me.errorsAndWarningsLabel.Name = "errorsAndWarningsLabel"
            '
            'lblSGenOption
            '
            resources.ApplyResources(Me.lblSGenOption, "lblSGenOption")
            Me.lblSGenOption.Name = "lblSGenOption"
            '
            'cboSGenOption
            '
            resources.ApplyResources(Me.cboSGenOption, "cboSGenOption")
            Me.cboSGenOption.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboSGenOption.FormattingEnabled = True
            Me.cboSGenOption.Name = "cboSGenOption"
            '
            'lblNullable
            '
            resources.ApplyResources(Me.lblNullable, "lblNullable")
            Me.lblNullable.Name = "lblNullable"
            '
            'cboNullable
            '
            resources.ApplyResources(Me.cboNullable, "cboNullable")
            Me.cboNullable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboNullable.FormattingEnabled = True
            Me.cboNullable.Name = "cboNullable"
            '
            'BuildPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "BuildPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.outputTableLayoutPanel.ResumeLayout(False)
            Me.outputTableLayoutPanel.PerformLayout()
            Me.generalHeaderTableLayoutPanel.ResumeLayout(False)
            Me.generalHeaderTableLayoutPanel.PerformLayout()
            Me.treatWarningsAsErrorsTableLayoutPanel.ResumeLayout(False)
            Me.treatWarningsAsErrorsTableLayoutPanel.PerformLayout()
            Me.errorsAndWarningsTableLayoutPanel.ResumeLayout(False)
            Me.errorsAndWarningsTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Friend WithEvents lblNullable As System.Windows.Forms.Label
        Friend WithEvents cboNullable As System.Windows.Forms.ComboBox
    End Class

End Namespace
