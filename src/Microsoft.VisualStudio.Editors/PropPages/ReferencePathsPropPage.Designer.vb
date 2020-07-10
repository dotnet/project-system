' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.Win32

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class ReferencePathsPropPage

        Friend WithEvents FolderLabel As System.Windows.Forms.Label
        Friend WithEvents Folder As System.Windows.Forms.TextBox
        Friend WithEvents FolderBrowse As System.Windows.Forms.Button
        Friend WithEvents AddFolder As System.Windows.Forms.Button
        Friend WithEvents UpdateFolder As System.Windows.Forms.Button
        Friend WithEvents ReferencePathLabel As System.Windows.Forms.Label
        Friend WithEvents ReferencePath As System.Windows.Forms.ListBox
        Friend WithEvents RemoveFolder As System.Windows.Forms.Button
        Friend WithEvents MoveUp As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents addUpdateTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents MoveDown As System.Windows.Forms.Button
        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
                RemoveHandler SystemEvents.UserPreferenceChanged, AddressOf Me.SystemEvents_UserPreferenceChanged
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ReferencePathsPropPage))
            Me.FolderLabel = New System.Windows.Forms.Label
            Me.Folder = New System.Windows.Forms.TextBox
            Me.FolderBrowse = New System.Windows.Forms.Button
            Me.AddFolder = New System.Windows.Forms.Button
            Me.UpdateFolder = New System.Windows.Forms.Button
            Me.ReferencePath = New System.Windows.Forms.ListBox
            Me.MoveUp = New System.Windows.Forms.Button
            Me.MoveDown = New System.Windows.Forms.Button
            Me.RemoveFolder = New System.Windows.Forms.Button
            Me.ReferencePathLabel = New System.Windows.Forms.Label
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.addUpdateTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.addUpdateTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'FolderLabel
            '
            resources.ApplyResources(Me.FolderLabel, "FolderLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.FolderLabel, 2)
            Me.FolderLabel.Margin = New System.Windows.Forms.Padding(0)
            Me.FolderLabel.Name = "FolderLabel"
            '
            'Folder
            '
            resources.ApplyResources(Me.Folder, "Folder")
            Me.Folder.Margin = New System.Windows.Forms.Padding(0, 3, 3, 2)
            Me.Folder.Name = "Folder"
            '
            'FolderBrowse
            '
            resources.ApplyResources(Me.FolderBrowse, "FolderBrowse")
            Me.FolderBrowse.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.FolderBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 0, 2)
            Me.FolderBrowse.Name = "FolderBrowse"
            '
            'AddFolder
            '
            resources.ApplyResources(Me.AddFolder, "AddFolder")
            Me.AddFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.AddFolder.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.AddFolder.Name = "AddFolder"
            '
            'UpdateFolder
            '
            resources.ApplyResources(Me.UpdateFolder, "UpdateFolder")
            Me.UpdateFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UpdateFolder.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.UpdateFolder.Name = "UpdateFolder"
            '
            'ReferencePath
            '
            resources.ApplyResources(Me.ReferencePath, "ReferencePath")
            Me.ReferencePath.FormattingEnabled = True
            Me.ReferencePath.Margin = New System.Windows.Forms.Padding(0, 3, 3, 0)
            Me.ReferencePath.Name = "ReferencePath"
            Me.overarchingTableLayoutPanel.SetRowSpan(Me.ReferencePath, 4)
            '
            'MoveUp
            '
            resources.ApplyResources(Me.MoveUp, "MoveUp")
            Me.MoveUp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.MoveUp.Margin = New System.Windows.Forms.Padding(3, 3, 0, 1)
            Me.MoveUp.Name = "MoveUp"
            '
            'MoveDown
            '
            resources.ApplyResources(Me.MoveDown, "MoveDown")
            Me.MoveDown.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.MoveDown.Margin = New System.Windows.Forms.Padding(3, 1, 0, 3)
            Me.MoveDown.Name = "MoveDown"
            '
            'RemoveFolder
            '
            resources.ApplyResources(Me.RemoveFolder, "RemoveFolder")
            Me.RemoveFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.RemoveFolder.Margin = New System.Windows.Forms.Padding(3, 3, 0, 0)
            Me.RemoveFolder.Name = "RemoveFolder"
            '
            'ReferencePathLabel
            '
            resources.ApplyResources(Me.ReferencePathLabel, "ReferencePathLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.ReferencePathLabel, 2)
            Me.ReferencePathLabel.Margin = New System.Windows.Forms.Padding(0, 9, 0, 0)
            Me.ReferencePathLabel.Name = "ReferencePathLabel"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.addUpdateTableLayoutPanel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FolderLabel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ReferencePath, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ReferencePathLabel, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Folder, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FolderBrowse, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.MoveUp, 1, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.MoveDown, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.RemoveFolder, 1, 6)
            Me.overarchingTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            '
            'addUpdateTableLayoutPanel
            '
            resources.ApplyResources(Me.addUpdateTableLayoutPanel, "addUpdateTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.addUpdateTableLayoutPanel, 2)
            Me.addUpdateTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.addUpdateTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.addUpdateTableLayoutPanel.Controls.Add(Me.AddFolder, 0, 0)
            Me.addUpdateTableLayoutPanel.Controls.Add(Me.UpdateFolder, 1, 0)
            Me.addUpdateTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 3, 0, 3)
            Me.addUpdateTableLayoutPanel.Name = "addUpdateTableLayoutPanel"
            Me.addUpdateTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'ReferencePathsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Margin = New System.Windows.Forms.Padding(0)
            Me.Name = "ReferencePathsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.addUpdateTableLayoutPanel.ResumeLayout(False)
            Me.addUpdateTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

    End Class

End Namespace
