' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class BuildEventCommandLineDialog

        Friend WithEvents OKButton As System.Windows.Forms.Button
        Friend WithEvents InsertButton As System.Windows.Forms.Button
        Friend WithEvents Cancel_Button As System.Windows.Forms.Button
        Friend WithEvents MacrosPanel As System.Windows.Forms.Panel
        Friend WithEvents CommandLinePanel As System.Windows.Forms.Panel
        Friend WithEvents HideMacrosButton As System.Windows.Forms.Button
        Friend WithEvents ShowMacrosButton As System.Windows.Forms.Button
        Friend WithEvents CommandLine As System.Windows.Forms.TextBox
        Friend WithEvents TokenList As System.Windows.Forms.ListView
        Friend WithEvents Macro As System.Windows.Forms.ColumnHeader
        Friend WithEvents Value As System.Windows.Forms.ColumnHeader
        Friend WithEvents insertOkCancelTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Private components As System.ComponentModel.IContainer


        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(BuildEventCommandLineDialog))
            Me.InsertButton = New System.Windows.Forms.Button()
            Me.OKButton = New System.Windows.Forms.Button()
            Me.Cancel_Button = New System.Windows.Forms.Button()
            Me.CommandLine = New System.Windows.Forms.TextBox()
            Me.ShowMacrosButton = New System.Windows.Forms.Button()
            Me.MacrosPanel = New System.Windows.Forms.Panel()
            Me.HideMacrosButton = New System.Windows.Forms.Button()
            Me.TokenList = New System.Windows.Forms.ListView()
            Me.Macro = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
            Me.Value = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
            Me.CommandLinePanel = New System.Windows.Forms.Panel()
            Me.insertOkCancelTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.MacrosPanel.SuspendLayout()
            Me.insertOkCancelTableLayoutPanel.SuspendLayout()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'InsertButton
            '
            resources.ApplyResources(Me.InsertButton, "InsertButton")
            Me.InsertButton.Name = "InsertButton"
            '
            'OKButton
            '
            resources.ApplyResources(Me.OKButton, "OKButton")
            Me.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK
            Me.OKButton.Name = "OKButton"
            '
            'Cancel_Button
            '
            resources.ApplyResources(Me.Cancel_Button, "Cancel_Button")
            Me.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.Cancel_Button.Name = "Cancel_Button"
            '
            'CommandLine
            '
            Me.CommandLine.AcceptsReturn = True
            resources.ApplyResources(Me.CommandLine, "CommandLine")
            Me.CommandLine.Name = "CommandLine"
            '
            'ShowMacrosButton
            '
            resources.ApplyResources(Me.ShowMacrosButton, "ShowMacrosButton")
            Me.ShowMacrosButton.Name = "ShowMacrosButton"
            '
            'MacrosPanel
            '
            resources.ApplyResources(Me.MacrosPanel, "MacrosPanel")
            Me.MacrosPanel.Controls.Add(Me.HideMacrosButton)
            Me.MacrosPanel.Controls.Add(Me.TokenList)
            Me.MacrosPanel.Name = "MacrosPanel"
            '
            'HideMacrosButton
            '
            resources.ApplyResources(Me.HideMacrosButton, "HideMacrosButton")
            Me.HideMacrosButton.Name = "HideMacrosButton"
            '
            'TokenList
            '
            resources.ApplyResources(Me.TokenList, "TokenList")
            Me.TokenList.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.Macro, Me.Value})
            Me.TokenList.MultiSelect = False
            Me.TokenList.Name = "TokenList"
            Me.TokenList.ShowItemToolTips = True
            Me.TokenList.UseCompatibleStateImageBehavior = False
            Me.TokenList.View = System.Windows.Forms.View.Details
            '
            'Macro
            '
            resources.ApplyResources(Me.Macro, "Macro")
            '
            'Value
            '
            resources.ApplyResources(Me.Value, "Value")
            '
            'CommandLinePanel
            '
            resources.ApplyResources(Me.CommandLinePanel, "CommandLinePanel")
            Me.CommandLinePanel.Name = "CommandLinePanel"
            '
            'insertOkCancelTableLayoutPanel
            '
            resources.ApplyResources(Me.insertOkCancelTableLayoutPanel, "insertOkCancelTableLayoutPanel")
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.ShowMacrosButton, 2, 0)
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.InsertButton, 0, 1)
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.OKButton, 1, 1)
            Me.insertOkCancelTableLayoutPanel.Controls.Add(Me.Cancel_Button, 2, 1)
            Me.insertOkCancelTableLayoutPanel.Name = "insertOkCancelTableLayoutPanel"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CommandLine, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.MacrosPanel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.insertOkCancelTableLayoutPanel, 0, 2)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'BuildEventCommandLineDialog
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = Me.Cancel_Button
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.HelpButton = True
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "BuildEventCommandLineDialog"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            Me.MacrosPanel.ResumeLayout(False)
            Me.MacrosPanel.PerformLayout()
            Me.insertOkCancelTableLayoutPanel.ResumeLayout(False)
            Me.insertOkCancelTableLayoutPanel.PerformLayout()
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

    End Class

End Namespace
