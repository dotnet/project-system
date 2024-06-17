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
        Friend WithEvents chkPreferNativeArm64 As System.Windows.Forms.CheckBox
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
        Friend WithEvents generalTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents errorsAndWarningsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents treatWarningsAsErrorsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents outputTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents generalGroupBox As SeparatorGroupBox
        Friend WithEvents errorsAndWarningsGroupBox As SeparatorGroupBox
        Friend WithEvents treatWarningsAsErrorsGroupBox As SeparatorGroupBox
        Friend WithEvents outputGroupBox As SeparatorGroupBox
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
            Me.chkPreferNativeArm64 = New System.Windows.Forms.CheckBox()
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
            Me.generalGroupBox = New SeparatorGroupBox()
            Me.errorsAndWarningsGroupBox = New SeparatorGroupBox()
            Me.treatWarningsAsErrorsGroupBox = New SeparatorGroupBox()
            Me.outputGroupBox = New SeparatorGroupBox()
            Me.generalTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.treatWarningsAsErrorsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.errorsAndWarningsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.lblSGenOption = New System.Windows.Forms.Label()
            Me.cboSGenOption = New System.Windows.Forms.ComboBox()
            Me.lblNullable = New System.Windows.Forms.Label()
            Me.cboNullable = New System.Windows.Forms.ComboBox()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.generalGroupBox.SuspendLayout()
            Me.outputTableLayoutPanel.SuspendLayout()
            Me.errorsAndWarningsGroupBox.SuspendLayout()
            Me.treatWarningsAsErrorsGroupBox.SuspendLayout()
            Me.outputGroupBox.SuspendLayout()
            Me.generalTableLayoutPanel.SuspendLayout()
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
            Me.generalTableLayoutPanel.SetColumnSpan(Me.chkDefineDebug, 3)
            Me.chkDefineDebug.Name = "chkDefineDebug"
            '
            'chkDefineTrace
            '
            resources.ApplyResources(Me.chkDefineTrace, "chkDefineTrace")
            Me.generalTableLayoutPanel.SetColumnSpan(Me.chkDefineTrace, 3)
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
            Me.generalTableLayoutPanel.SetColumnSpan(Me.chkPrefer32Bit, 3)
            Me.chkPrefer32Bit.Name = "chkPrefer32Bit"
            '
            'chkPreferNativeArm64
            '
            resources.ApplyResources(Me.chkPreferNativeArm64, "chkPreferNativeArm64")
            Me.generalTableLayoutPanel.SetColumnSpan(Me.chkPreferNativeArm64, 3)
            Me.chkPreferNativeArm64.Name = "chkPreferNativeArm64"
            '
            'chkAllowUnsafeCode
            '
            resources.ApplyResources(Me.chkAllowUnsafeCode, "chkAllowUnsafeCode")
            Me.generalTableLayoutPanel.SetColumnSpan(Me.chkAllowUnsafeCode, 3)
            Me.chkAllowUnsafeCode.Name = "chkAllowUnsafeCode"
            '
            'chkOptimizeCode
            '
            resources.ApplyResources(Me.chkOptimizeCode, "chkOptimizeCode")
            Me.generalTableLayoutPanel.SetColumnSpan(Me.chkOptimizeCode, 3)
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
            Me.treatWarningsAsErrorsTableLayoutPanel.SetColumnSpan(Me.rbWarningNone, 3)
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
            Me.treatWarningsAsErrorsTableLayoutPanel.SetColumnSpan(Me.rbWarningAll, 3)
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
            Me.outputTableLayoutPanel.SetColumnSpan(Me.chkRegisterForCOM, 3)
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
            Me.overarchingTableLayoutPanel.Controls.Add(Me.generalGroupBox, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.errorsAndWarningsGroupBox, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.treatWarningsAsErrorsGroupBox, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.outputGroupBox, 0, 3)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'generalTableLayoutPanel
            '
            resources.ApplyResources(Me.generalTableLayoutPanel, "generalTableLayoutPanel")
            Me.generalTableLayoutPanel.Controls.Add(Me.txtConditionalCompilationSymbols, 1, 0)
            Me.generalTableLayoutPanel.Controls.Add(Me.lblConditionalCompilationSymbols, 0, 0)
            Me.generalTableLayoutPanel.Controls.Add(Me.chkDefineDebug, 0, 1)
            Me.generalTableLayoutPanel.Controls.Add(Me.chkDefineTrace, 0, 2)
            Me.generalTableLayoutPanel.Controls.Add(Me.lblPlatformTarget, 0, 3)
            Me.generalTableLayoutPanel.Controls.Add(Me.cboPlatformTarget, 1, 3)
            Me.generalTableLayoutPanel.Controls.Add(Me.lblNullable, 0, 4)
            Me.generalTableLayoutPanel.Controls.Add(Me.cboNullable, 1, 4)
            Me.generalTableLayoutPanel.Controls.Add(Me.chkPrefer32Bit, 0, 5)
            Me.generalTableLayoutPanel.Controls.Add(Me.chkPreferNativeArm64, 0, 6)
            Me.generalTableLayoutPanel.Controls.Add(Me.chkAllowUnsafeCode, 0, 7)
            Me.generalTableLayoutPanel.Controls.Add(Me.chkOptimizeCode, 0, 8)
            Me.generalTableLayoutPanel.Name = "generalTableLayoutPanel"
            '
            'errorsAndWarningsTableLayoutPanel
            '
            resources.ApplyResources(Me.errorsAndWarningsTableLayoutPanel, "errorsAndWarningsTableLayoutPanel")
            Me.errorsAndWarningsTableLayoutPanel.Controls.Add(Me.txtSupressWarnings, 1, 1)
            Me.errorsAndWarningsTableLayoutPanel.Controls.Add(Me.lblSupressWarnings, 0, 1)
            Me.errorsAndWarningsTableLayoutPanel.Controls.Add(Me.cboWarningLevel, 1, 0)
            Me.errorsAndWarningsTableLayoutPanel.Controls.Add(Me.lblWarningLevel, 0, 0)
            Me.errorsAndWarningsTableLayoutPanel.Name = "errorsAndWarningsTableLayoutPanel"
            '
            'treatWarningsAsErrorsTableLayoutPanel
            '
            resources.ApplyResources(Me.treatWarningsAsErrorsTableLayoutPanel, "treatWarningsAsErrorsTableLayoutPanel")
            Me.treatWarningsAsErrorsTableLayoutPanel.Controls.Add(Me.rbWarningNone, 0, 0)
            Me.treatWarningsAsErrorsTableLayoutPanel.Controls.Add(Me.rbWarningAll, 0, 1)
            Me.treatWarningsAsErrorsTableLayoutPanel.Controls.Add(Me.rbWarningSpecific, 0, 2)
            Me.treatWarningsAsErrorsTableLayoutPanel.Controls.Add(Me.txtSpecificWarnings, 1, 2)
            Me.treatWarningsAsErrorsTableLayoutPanel.Name = "treatWarningsAsErrorsTableLayoutPanel"
            '
            'outputTableLayoutPanel
            '
            resources.ApplyResources(Me.outputTableLayoutPanel, "outputTableLayoutPanel")
            Me.outputTableLayoutPanel.Controls.Add(Me.lblOutputPath, 0, 0)
            Me.outputTableLayoutPanel.Controls.Add(Me.txtOutputPath, 1, 0)
            Me.outputTableLayoutPanel.Controls.Add(Me.btnOutputPathBrowse, 2, 0)
            Me.outputTableLayoutPanel.Controls.Add(Me.chkXMLDocumentationFile, 0, 1)
            Me.outputTableLayoutPanel.Controls.Add(Me.txtXMLDocumentationFile, 1, 1)
            Me.outputTableLayoutPanel.Controls.Add(Me.chkRegisterForCOM, 0, 2)
            Me.outputTableLayoutPanel.Controls.Add(Me.lblSGenOption, 0, 3)
            Me.outputTableLayoutPanel.Controls.Add(Me.cboSGenOption, 1, 3)
            Me.outputTableLayoutPanel.Controls.Add(Me.btnAdvanced, 2, 4)
            Me.outputTableLayoutPanel.Name = "outputTableLayoutPanel"
            '
            'generalGroupBox
            '
            resources.ApplyResources(Me.generalGroupBox, "generalGroupBox")
            Me.generalGroupBox.Controls.Add(Me.generalTableLayoutPanel)
            Me.generalGroupBox.Name = "generalGroupBox"
            '
            'errorsAndWarningsGroupBox
            '
            resources.ApplyResources(Me.errorsAndWarningsGroupBox, "errorsAndWarningsGroupBox")
            Me.errorsAndWarningsGroupBox.Controls.Add(Me.errorsAndWarningsTableLayoutPanel)
            Me.errorsAndWarningsGroupBox.Name = "errorsAndWarningsGroupBox"
            '
            'treatWarningsAsErrorsGroupBox
            '
            resources.ApplyResources(Me.treatWarningsAsErrorsGroupBox, "treatWarningsAsErrorsGroupBox")
            Me.treatWarningsAsErrorsGroupBox.Controls.Add(Me.treatWarningsAsErrorsTableLayoutPanel)
            Me.treatWarningsAsErrorsGroupBox.Name = "treatWarningsAsErrorsGroupBox"
            '
            'outputGroupBox
            '
            resources.ApplyResources(Me.outputGroupBox, "outputGroupBox")
            Me.outputGroupBox.Controls.Add(Me.outputTableLayoutPanel)
            Me.outputGroupBox.Name = "outputGroupBox"
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
            Me.outputGroupBox.ResumeLayout(False)
            Me.outputGroupBox.PerformLayout()
            Me.generalGroupBox.ResumeLayout(False)
            Me.generalGroupBox.PerformLayout()
            Me.generalTableLayoutPanel.ResumeLayout(False)
            Me.generalTableLayoutPanel.PerformLayout()
            Me.treatWarningsAsErrorsTableLayoutPanel.ResumeLayout(False)
            Me.treatWarningsAsErrorsTableLayoutPanel.PerformLayout()
            Me.treatWarningsAsErrorsGroupBox.ResumeLayout(False)
            Me.treatWarningsAsErrorsGroupBox.PerformLayout()
            Me.errorsAndWarningsTableLayoutPanel.ResumeLayout(False)
            Me.errorsAndWarningsTableLayoutPanel.PerformLayout()
            Me.errorsAndWarningsGroupBox.ResumeLayout(False)
            Me.errorsAndWarningsGroupBox.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Friend WithEvents lblNullable As System.Windows.Forms.Label
        Friend WithEvents cboNullable As System.Windows.Forms.ComboBox
    End Class

End Namespace
