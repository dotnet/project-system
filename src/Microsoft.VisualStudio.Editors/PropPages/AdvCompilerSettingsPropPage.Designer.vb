' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class AdvCompilerSettingsPropPage

        Friend WithEvents RemoveIntegerChecks As System.Windows.Forms.CheckBox
        Friend WithEvents Optimize As System.Windows.Forms.CheckBox
        Friend WithEvents DefineDebug As System.Windows.Forms.CheckBox
        Friend WithEvents DefineTrace As System.Windows.Forms.CheckBox
        Friend WithEvents DllBaseLabel As System.Windows.Forms.Label
        Friend WithEvents CustomConstantsExampleLabel As System.Windows.Forms.Label
        Friend WithEvents DefineConstantsTextbox As System.Windows.Forms.TextBox
        Friend WithEvents OptimizationsSeparatorLabel As System.Windows.Forms.Label
        Friend WithEvents CompilationConstantsLabel As System.Windows.Forms.Label
        Friend WithEvents OptimizationsLabel As System.Windows.Forms.Label
        Friend WithEvents DllBaseTextbox As System.Windows.Forms.TextBox
        Friend WithEvents ConstantsSeparatorLabel As System.Windows.Forms.Label
        Friend WithEvents GenerateSerializationAssembliesLabel As System.Windows.Forms.Label
        Friend WithEvents GenerateSerializationAssemblyComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents optimizationTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents compilationConstantsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents GenerateDebugInfoLabel As System.Windows.Forms.Label
        Friend WithEvents DebugInfoComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents CustomConstantsLabel As System.Windows.Forms.Label
        Friend WithEvents CompileWithDotNetNative As System.Windows.Forms.CheckBox
        Friend WithEvents EnableGatekeeperAnAlysis As System.Windows.Forms.CheckBox
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvCompilerSettingsPropPage))
            Me.OptimizationsLabel = New System.Windows.Forms.Label
            Me.RemoveIntegerChecks = New System.Windows.Forms.CheckBox
            Me.Optimize = New System.Windows.Forms.CheckBox
            Me.DllBaseLabel = New System.Windows.Forms.Label
            Me.DllBaseTextbox = New System.Windows.Forms.TextBox
            Me.DefineDebug = New System.Windows.Forms.CheckBox
            Me.DefineTrace = New System.Windows.Forms.CheckBox
            Me.CustomConstantsExampleLabel = New System.Windows.Forms.Label
            Me.DefineConstantsTextbox = New System.Windows.Forms.TextBox
            Me.CustomConstantsLabel = New System.Windows.Forms.Label
            Me.OptimizationsSeparatorLabel = New System.Windows.Forms.Label
            Me.CompilationConstantsLabel = New System.Windows.Forms.Label
            Me.ConstantsSeparatorLabel = New System.Windows.Forms.Label
            Me.GenerateSerializationAssembliesLabel = New System.Windows.Forms.Label
            Me.GenerateSerializationAssemblyComboBox = New System.Windows.Forms.ComboBox
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.compilationConstantsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.optimizationTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.GenerateDebugInfoLabel = New System.Windows.Forms.Label
            Me.DebugInfoComboBox = New System.Windows.Forms.ComboBox
            Me.CompileWithDotNetNative = New System.Windows.Forms.CheckBox()
            Me.EnableGatekeeperAnAlysis = New System.Windows.Forms.CheckBox()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.compilationConstantsTableLayoutPanel.SuspendLayout()
            Me.optimizationTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'OptimizationsLabel
            '
            resources.ApplyResources(Me.OptimizationsLabel, "OptimizationsLabel")
            Me.OptimizationsLabel.Name = "OptimizationsLabel"
            '
            'RemoveIntegerChecks
            '
            resources.ApplyResources(Me.RemoveIntegerChecks, "RemoveIntegerChecks")
            Me.RemoveIntegerChecks.Name = "RemoveIntegerChecks"

            '
            ' CompileWithDotNetNative
            '
            resources.ApplyResources(Me.CompileWithDotNetNative, "CompileWithDotNetNative")
            Me.CompileWithDotNetNative.Name = "CompileWithDotNetNative"
            resources.ApplyResources(Me.EnableGatekeeperAnAlysis, "EnableGatekeeperAnalysis")
            Me.EnableGatekeeperAnAlysis.Name = "EnableGatekeeperAnalysis"

            '
            'Optimize
            '
            resources.ApplyResources(Me.Optimize, "Optimize")
            Me.Optimize.Name = "Optimize"
            '
            'DllBaseLabel
            '
            resources.ApplyResources(Me.DllBaseLabel, "DllBaseLabel")
            Me.DllBaseLabel.Name = "DllBaseLabel"
            '
            'DllBaseTextbox
            '
            resources.ApplyResources(Me.DllBaseTextbox, "DllBaseTextbox")
            Me.DllBaseTextbox.Name = "DllBaseTextbox"
            '
            'DefineDebug
            '
            resources.ApplyResources(Me.DefineDebug, "DefineDebug")
            Me.DefineDebug.Name = "DefineDebug"
            '
            'DefineTrace
            '
            resources.ApplyResources(Me.DefineTrace, "DefineTrace")
            Me.DefineTrace.Name = "DefineTrace"
            '
            'CustomConstantsExampleLabel
            '
            resources.ApplyResources(Me.CustomConstantsExampleLabel, "CustomConstantsExampleLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.CustomConstantsExampleLabel, 2)
            Me.CustomConstantsExampleLabel.Name = "CustomConstantsExampleLabel"
            '
            'DefineConstantsTextbox
            '
            resources.ApplyResources(Me.DefineConstantsTextbox, "DefineConstantsTextbox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.DefineConstantsTextbox, 2)
            Me.DefineConstantsTextbox.Name = "DefineConstantsTextbox"
            '
            'CustomConstantsLabel
            '
            resources.ApplyResources(Me.CustomConstantsLabel, "CustomConstantsLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.CustomConstantsLabel, 2)
            Me.CustomConstantsLabel.Name = "CustomConstantsLabel"
            '
            'OptimizationsSeparatorLabel
            '
            Me.OptimizationsSeparatorLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.OptimizationsSeparatorLabel, "OptimizationsSeparatorLabel")
            Me.OptimizationsSeparatorLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.OptimizationsSeparatorLabel.Name = "OptimizationsSeparatorLabel"
            '
            'CompilationConstantsLabel
            '
            resources.ApplyResources(Me.CompilationConstantsLabel, "CompilationConstantsLabel")
            Me.CompilationConstantsLabel.Name = "CompilationConstantsLabel"
            '
            'ConstantsSeparatorLabel
            '
            Me.ConstantsSeparatorLabel.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.ConstantsSeparatorLabel, "ConstantsSeparatorLabel")
            Me.ConstantsSeparatorLabel.BackColor = System.Drawing.SystemColors.ControlDark
            Me.ConstantsSeparatorLabel.Name = "ConstantsSeparatorLabel"
            '
            'GenerateSerializationAssembliesLabel
            '
            resources.ApplyResources(Me.GenerateSerializationAssembliesLabel, "GenerateSerializationAssembliesLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.GenerateSerializationAssembliesLabel, 2)
            Me.GenerateSerializationAssembliesLabel.Name = "GenerateSerializationAssembliesLabel"
            '
            'GenerateSerializationAssemblyComboBox
            '
            resources.ApplyResources(Me.GenerateSerializationAssemblyComboBox, "GenerateSerializationAssemblyComboBox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.GenerateSerializationAssemblyComboBox, 2)
            Me.GenerateSerializationAssemblyComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.GenerateSerializationAssemblyComboBox.FormattingEnabled = True
            Me.GenerateSerializationAssemblyComboBox.Name = "GenerateSerializationAssemblyComboBox"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CustomConstantsExampleLabel, 0, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GenerateSerializationAssemblyComboBox, 0, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DefineConstantsTextbox, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GenerateSerializationAssembliesLabel, 0, 9)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CustomConstantsLabel, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.compilationConstantsTableLayoutPanel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DllBaseLabel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.optimizationTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.RemoveIntegerChecks, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DefineDebug, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GenerateDebugInfoLabel, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DebugInfoComboBox, 1, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DefineTrace, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Optimize, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DllBaseTextbox, 1, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CompileWithDotNetNative, 0, 12)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.EnableGatekeeperAnAlysis, 0, 13)

            '
            'compilationConstantsTableLayoutPanel
            '
            resources.ApplyResources(Me.compilationConstantsTableLayoutPanel, "compilationConstantsTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.compilationConstantsTableLayoutPanel, 2)
            Me.compilationConstantsTableLayoutPanel.Controls.Add(Me.CompilationConstantsLabel, 0, 0)
            Me.compilationConstantsTableLayoutPanel.Controls.Add(Me.ConstantsSeparatorLabel, 1, 0)
            Me.compilationConstantsTableLayoutPanel.Name = "compilationConstantsTableLayoutPanel"
            '
            'optimizationTableLayoutPanel
            '
            resources.ApplyResources(Me.optimizationTableLayoutPanel, "optimizationTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.optimizationTableLayoutPanel, 2)
            Me.optimizationTableLayoutPanel.Controls.Add(Me.OptimizationsLabel, 0, 0)
            Me.optimizationTableLayoutPanel.Controls.Add(Me.OptimizationsSeparatorLabel, 1, 0)
            Me.optimizationTableLayoutPanel.Name = "optimizationTableLayoutPanel"
            '
            'GenerateDebugInfoLabel
            '
            resources.ApplyResources(Me.GenerateDebugInfoLabel, "GenerateDebugInfoLabel")
            Me.GenerateDebugInfoLabel.Name = "GenerateDebugInfoLabel"
            '
            'DebugInfoComboBox
            '
            resources.ApplyResources(Me.DebugInfoComboBox, "DebugInfoComboBox")
            Me.DebugInfoComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.DebugInfoComboBox.FormattingEnabled = True
            Me.DebugInfoComboBox.Name = "DebugInfoComboBox"
            '
            'AdvCompilerSettingsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AdvCompilerSettingsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.compilationConstantsTableLayoutPanel.ResumeLayout(False)
            Me.compilationConstantsTableLayoutPanel.PerformLayout()
            Me.optimizationTableLayoutPanel.ResumeLayout(False)
            Me.optimizationTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

    End Class

End Namespace
