' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Class DebugPropPage

        Friend WithEvents rbStartProject As System.Windows.Forms.RadioButton
        Friend WithEvents rbStartProgram As System.Windows.Forms.RadioButton
        Friend WithEvents rbStartURL As System.Windows.Forms.RadioButton
        Friend WithEvents StartProgram As TextBoxWithWorkaroundForAutoCompleteAppend
        Friend WithEvents StartURL As TextBoxWithWorkaroundForAutoCompleteAppend
        Friend WithEvents RemoteDebugEnabled As System.Windows.Forms.CheckBox
        Friend WithEvents StartArguments As MultilineTextBoxRejectsEnter
        Friend WithEvents StartWorkingDirectory As TextBoxWithWorkaroundForAutoCompleteAppend
        Friend WithEvents RemoteDebugMachine As System.Windows.Forms.TextBox
        Friend WithEvents AuthenticationModeLabel As System.Windows.Forms.Label
        Friend WithEvents AuthenticationMode As System.Windows.Forms.ComboBox
        Friend WithEvents EnableUnmanagedDebugging As System.Windows.Forms.CheckBox
        Friend WithEvents StartProgramBrowse As System.Windows.Forms.Button
        Friend WithEvents StartWorkingDirectoryBrowse As System.Windows.Forms.Button
        Friend WithEvents StartOptionsLabel As System.Windows.Forms.Label
        Friend WithEvents CommandLineArgsLabel As System.Windows.Forms.Label
        Friend WithEvents WorkingDirLabel As System.Windows.Forms.Label
        Friend WithEvents EnableDebuggerLabelLine As System.Windows.Forms.Label
        Friend WithEvents EnableDebuggerLabel As System.Windows.Forms.Label
        Friend WithEvents StartActionLabel As System.Windows.Forms.Label
        Friend WithEvents StartActionLabelLine As System.Windows.Forms.Label
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents startActionTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents startOptionsTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents enableDebuggersTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents StartOptionsLabelLine As System.Windows.Forms.Label
        Friend WithEvents EnableSQLServerDebugging As System.Windows.Forms.CheckBox
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(DebugPropPage))
            Me.StartActionLabel = New System.Windows.Forms.Label()
            Me.StartActionLabelLine = New System.Windows.Forms.Label()
            Me.rbStartProject = New System.Windows.Forms.RadioButton()
            Me.rbStartProgram = New System.Windows.Forms.RadioButton()
            Me.rbStartURL = New System.Windows.Forms.RadioButton()
            Me.StartProgram = New Microsoft.VisualStudio.Editors.PropertyPages.TextBoxWithWorkaroundForAutoCompleteAppend()
            Me.StartURL = New Microsoft.VisualStudio.Editors.PropertyPages.TextBoxWithWorkaroundForAutoCompleteAppend()
            Me.StartProgramBrowse = New System.Windows.Forms.Button()
            Me.StartOptionsLabelLine = New System.Windows.Forms.Label()
            Me.StartOptionsLabel = New System.Windows.Forms.Label()
            Me.CommandLineArgsLabel = New System.Windows.Forms.Label()
            Me.WorkingDirLabel = New System.Windows.Forms.Label()
            Me.RemoteDebugEnabled = New System.Windows.Forms.CheckBox()
            Me.StartArguments = New Microsoft.VisualStudio.Editors.PropertyPages.DebugPropPage.MultilineTextBoxRejectsEnter()
            Me.StartWorkingDirectory = New Microsoft.VisualStudio.Editors.PropertyPages.TextBoxWithWorkaroundForAutoCompleteAppend()
            Me.RemoteDebugMachine = New System.Windows.Forms.TextBox()
            Me.AuthenticationModeLabel = New System.Windows.Forms.Label()
            Me.AuthenticationMode = New System.Windows.Forms.ComboBox()
            Me.StartWorkingDirectoryBrowse = New System.Windows.Forms.Button()
            Me.EnableDebuggerLabelLine = New System.Windows.Forms.Label()
            Me.EnableDebuggerLabel = New System.Windows.Forms.Label()
            Me.EnableUnmanagedDebugging = New System.Windows.Forms.CheckBox()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.startActionTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.startOptionsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.enableDebuggersTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.EnableSQLServerDebugging = New System.Windows.Forms.CheckBox()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.startActionTableLayoutPanel.SuspendLayout()
            Me.startOptionsTableLayoutPanel.SuspendLayout()
            Me.enableDebuggersTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'StartActionLabel
            '
            resources.ApplyResources(Me.StartActionLabel, "StartActionLabel")
            Me.StartActionLabel.Name = "StartActionLabel"
            '
            'StartActionLabelLine
            '
            Me.StartActionLabelLine.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.StartActionLabelLine, "StartActionLabelLine")
            Me.StartActionLabelLine.BackColor = System.Drawing.SystemColors.ControlDark
            Me.StartActionLabelLine.Name = "StartActionLabelLine"
            '
            'rbStartProject
            '
            resources.ApplyResources(Me.rbStartProject, "rbStartProject")
            Me.rbStartProject.Name = "rbStartProject"
            '
            'rbStartProgram
            '
            resources.ApplyResources(Me.rbStartProgram, "rbStartProgram")
            Me.rbStartProgram.Name = "rbStartProgram"
            '
            'rbStartURL
            '
            resources.ApplyResources(Me.rbStartURL, "rbStartURL")
            Me.rbStartURL.Name = "rbStartURL"
            '
            'StartProgram
            '
            resources.ApplyResources(Me.StartProgram, "StartProgram")
            Me.StartProgram.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.StartProgram.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem
            Me.StartProgram.Name = "StartProgram"
            '
            'StartURL
            '
            resources.ApplyResources(Me.StartURL, "StartURL")
            Me.StartURL.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.StartURL.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.RecentlyUsedList
            Me.StartURL.Name = "StartURL"
            '
            'StartProgramBrowse
            '
            resources.ApplyResources(Me.StartProgramBrowse, "StartProgramBrowse")
            Me.StartProgramBrowse.Name = "StartProgramBrowse"
            '
            'StartOptionsLabelLine
            '
            Me.StartOptionsLabelLine.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.StartOptionsLabelLine, "StartOptionsLabelLine")
            Me.StartOptionsLabelLine.BackColor = System.Drawing.SystemColors.ControlDark
            Me.StartOptionsLabelLine.Name = "StartOptionsLabelLine"
            '
            'StartOptionsLabel
            '
            resources.ApplyResources(Me.StartOptionsLabel, "StartOptionsLabel")
            Me.StartOptionsLabel.Name = "StartOptionsLabel"
            '
            'CommandLineArgsLabel
            '
            resources.ApplyResources(Me.CommandLineArgsLabel, "CommandLineArgsLabel")
            Me.CommandLineArgsLabel.Name = "CommandLineArgsLabel"
            '
            'WorkingDirLabel
            '
            resources.ApplyResources(Me.WorkingDirLabel, "WorkingDirLabel")
            Me.WorkingDirLabel.Name = "WorkingDirLabel"
            '
            'RemoteDebugEnabled
            '
            resources.ApplyResources(Me.RemoteDebugEnabled, "RemoteDebugEnabled")
            Me.RemoteDebugEnabled.Name = "RemoteDebugEnabled"
            '
            'StartArguments
            '
            resources.ApplyResources(Me.StartArguments, "StartArguments")
            Me.StartArguments.Name = "StartArguments"
            '
            'StartWorkingDirectory
            '
            resources.ApplyResources(Me.StartWorkingDirectory, "StartWorkingDirectory")
            Me.StartWorkingDirectory.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
            Me.StartWorkingDirectory.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem
            Me.StartWorkingDirectory.Name = "StartWorkingDirectory"
            '
            'RemoteDebugMachine
            '
            resources.ApplyResources(Me.RemoteDebugMachine, "RemoteDebugMachine")
            Me.RemoteDebugMachine.Name = "RemoteDebugMachine"
            '
            'AuthenticationModeLabel
            '
            resources.ApplyResources(Me.AuthenticationModeLabel, "AuthenticationModeLabel")
            Me.AuthenticationModeLabel.Name = "AuthenticationModeLabel"
            '
            'AuthenticationMode
            '
            resources.ApplyResources(Me.AuthenticationMode, "AuthenticationMode")
            Me.AuthenticationMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.AuthenticationMode.Name = "AuthenticationMode"
            '
            'StartWorkingDirectoryBrowse
            '
            resources.ApplyResources(Me.StartWorkingDirectoryBrowse, "StartWorkingDirectoryBrowse")
            Me.StartWorkingDirectoryBrowse.Name = "StartWorkingDirectoryBrowse"
            '
            'EnableDebuggerLabelLine
            '
            Me.EnableDebuggerLabelLine.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.EnableDebuggerLabelLine, "EnableDebuggerLabelLine")
            Me.EnableDebuggerLabelLine.BackColor = System.Drawing.SystemColors.ControlDark
            Me.EnableDebuggerLabelLine.Name = "EnableDebuggerLabelLine"
            '
            'EnableDebuggerLabel
            '
            resources.ApplyResources(Me.EnableDebuggerLabel, "EnableDebuggerLabel")
            Me.EnableDebuggerLabel.Name = "EnableDebuggerLabel"
            '
            'EnableUnmanagedDebugging
            '
            resources.ApplyResources(Me.EnableUnmanagedDebugging, "EnableUnmanagedDebugging")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.EnableUnmanagedDebugging, 2)
            Me.EnableUnmanagedDebugging.Name = "EnableUnmanagedDebugging"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.startActionTableLayoutPanel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.rbStartProject, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.rbStartProgram, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.StartProgram, 1, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.StartProgramBrowse, 2, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.rbStartURL, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.startOptionsTableLayoutPanel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CommandLineArgsLabel, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.StartArguments, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.WorkingDirLabel, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.StartWorkingDirectory, 1, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.StartWorkingDirectoryBrowse, 2, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.RemoteDebugEnabled, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.enableDebuggersTableLayoutPanel, 0, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.EnableUnmanagedDebugging, 0, 11)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.EnableSQLServerDebugging, 0, 12)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.StartURL, 1, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.RemoteDebugMachine, 1, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.AuthenticationModeLabel, 0, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.AuthenticationMode, 1, 8)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'startActionTableLayoutPanel
            '
            resources.ApplyResources(Me.startActionTableLayoutPanel, "startActionTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.startActionTableLayoutPanel, 3)
            Me.startActionTableLayoutPanel.Controls.Add(Me.StartActionLabel, 0, 0)
            Me.startActionTableLayoutPanel.Controls.Add(Me.StartActionLabelLine, 1, 0)
            Me.startActionTableLayoutPanel.Name = "startActionTableLayoutPanel"
            '
            'startOptionsTableLayoutPanel
            '
            resources.ApplyResources(Me.startOptionsTableLayoutPanel, "startOptionsTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.startOptionsTableLayoutPanel, 3)
            Me.startOptionsTableLayoutPanel.Controls.Add(Me.StartOptionsLabel, 0, 0)
            Me.startOptionsTableLayoutPanel.Controls.Add(Me.StartOptionsLabelLine, 1, 0)
            Me.startOptionsTableLayoutPanel.Name = "startOptionsTableLayoutPanel"
            '
            'enableDebuggersTableLayoutPanel
            '
            resources.ApplyResources(Me.enableDebuggersTableLayoutPanel, "enableDebuggersTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.enableDebuggersTableLayoutPanel, 3)
            Me.enableDebuggersTableLayoutPanel.Controls.Add(Me.EnableDebuggerLabel, 0, 0)
            Me.enableDebuggersTableLayoutPanel.Controls.Add(Me.EnableDebuggerLabelLine, 1, 0)
            Me.enableDebuggersTableLayoutPanel.Name = "enableDebuggersTableLayoutPanel"
            '
            'EnableSQLServerDebugging
            '
            resources.ApplyResources(Me.EnableSQLServerDebugging, "EnableSQLServerDebugging")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.EnableSQLServerDebugging, 2)
            Me.EnableSQLServerDebugging.Name = "EnableSQLServerDebugging"
            '
            'DebugPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "DebugPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.startActionTableLayoutPanel.ResumeLayout(False)
            Me.startActionTableLayoutPanel.PerformLayout()
            Me.startOptionsTableLayoutPanel.ResumeLayout(False)
            Me.startOptionsTableLayoutPanel.PerformLayout()
            Me.enableDebuggersTableLayoutPanel.ResumeLayout(False)
            Me.enableDebuggersTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

    End Class

End Namespace

