' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class ReferencePropPage

        Friend WithEvents ReferenceList As System.Windows.Forms.ListView
        Friend WithEvents RemoveReference As System.Windows.Forms.Button
        Friend WithEvents UpdateReferences As System.Windows.Forms.Button
        Friend WithEvents ColHdr_RefName As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Path As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Type As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Version As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_CopyLocal As System.Windows.Forms.ColumnHeader
        Friend WithEvents ImportList As System.Windows.Forms.CheckedListBox
        Friend WithEvents ReferenceListLabel As System.Windows.Forms.Label
        Friend WithEvents ImportsListLabel As System.Windows.Forms.Label
        Friend WithEvents UnusedReferences As System.Windows.Forms.Button
        Friend WithEvents addSplitButton As Microsoft.VisualStudio.Editors.Common.SplitButton
        Friend WithEvents addContextMenuStrip As System.Windows.Forms.ContextMenuStrip
        Friend WithEvents referenceToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents webReferenceToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents serviceReferenceToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
        Friend WithEvents ReferencePageTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents ReferencePathsButton As System.Windows.Forms.Button
        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me._components = New System.ComponentModel.Container
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ReferencePropPage))
            Me.ReferenceListLabel = New System.Windows.Forms.Label
            Me.ReferenceList = New System.Windows.Forms.ListView
            Me.ColHdr_RefName = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns"))
            Me.ColHdr_Type = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns1"))
            Me.ColHdr_Version = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns2"))
            Me.ColHdr_CopyLocal = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns3"))
            Me.ColHdr_Path = New System.Windows.Forms.ColumnHeader(resources.GetString("ReferenceList.Columns4"))
            Me.ReferencePageTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.referenceButtonsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.UnusedReferences = New System.Windows.Forms.Button
            Me.ReferencePathsButton = New System.Windows.Forms.Button
            Me.addRemoveButtonsTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.RemoveReference = New System.Windows.Forms.Button
            Me.addSplitButton = New Microsoft.VisualStudio.Editors.Common.SplitButton
            Me.addContextMenuStrip = New System.Windows.Forms.ContextMenuStrip(Me._components)
            Me.referenceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.webReferenceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.serviceReferenceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
            Me.UpdateReferences = New System.Windows.Forms.Button
            Me.ImportsListLabel = New System.Windows.Forms.Label
            Me.AddUserImportButton = New System.Windows.Forms.Button
            Me.UserImportTextBox = New System.Windows.Forms.TextBox
            Me.ImportList = New System.Windows.Forms.CheckedListBox
            Me.addUserImportTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.UpdateUserImportButton = New System.Windows.Forms.Button
            Me.ReferencePageSplitContainer = New System.Windows.Forms.SplitContainer
            Me.ReferencePageTableLayoutPanel.SuspendLayout()
            Me.referenceButtonsTableLayoutPanel.SuspendLayout()
            Me.addRemoveButtonsTableLayoutPanel.SuspendLayout()
            Me.addContextMenuStrip.SuspendLayout()
            Me.addUserImportTableLayoutPanel.SuspendLayout()
            Me.ReferencePageSplitContainer.Panel1.SuspendLayout()
            Me.ReferencePageSplitContainer.Panel2.SuspendLayout()
            Me.ReferencePageSplitContainer.SuspendLayout()
            Me.SuspendLayout()
            '
            'ReferenceListLabel
            '
            resources.ApplyResources(Me.ReferenceListLabel, "ReferenceListLabel")
            Me.ReferenceListLabel.Margin = New System.Windows.Forms.Padding(0)
            Me.ReferenceListLabel.Name = "ReferenceListLabel"
            '
            'ReferenceList
            '
            Me.ReferenceList.AutoArrange = False
            Me.ReferenceList.BackColor = System.Drawing.SystemColors.Window
            Me.ReferenceList.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColHdr_RefName, Me.ColHdr_Type, Me.ColHdr_Version, Me.ColHdr_CopyLocal, Me.ColHdr_Path})
            Me.ReferencePageTableLayoutPanel.SetColumnSpan(Me.ReferenceList, 2)
            resources.ApplyResources(Me.ReferenceList, "ReferenceList")
            Me.ReferenceList.FullRowSelect = True
            Me.ReferenceList.HideSelection = False
            Me.ReferenceList.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.ReferenceList.Name = "ReferenceList"
            Me.ReferenceList.ShowItemToolTips = True
            '
            'ColHdr_RefName
            '
            resources.ApplyResources(Me.ColHdr_RefName, "ColHdr_RefName")
            '
            'ColHdr_Type
            '
            resources.ApplyResources(Me.ColHdr_Type, "ColHdr_Type")
            '
            'ColHdr_Version
            '
            resources.ApplyResources(Me.ColHdr_Version, "ColHdr_Version")
            '
            'ColHdr_CopyLocal
            '
            resources.ApplyResources(Me.ColHdr_CopyLocal, "ColHdr_CopyLocal")
            '
            'ColHdr_Path
            '
            resources.ApplyResources(Me.ColHdr_Path, "ColHdr_Path")
            '
            'ReferencePageTableLayoutPanel
            '
            resources.ApplyResources(Me.ReferencePageTableLayoutPanel, "ReferencePageTableLayoutPanel")
            Me.ReferencePageTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.ReferencePageTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.referenceButtonsTableLayoutPanel, 1, 0)
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.ReferenceListLabel, 0, 0)
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.ReferenceList, 0, 1)
            Me.ReferencePageTableLayoutPanel.Controls.Add(Me.addRemoveButtonsTableLayoutPanel, 0, 2)
            Me.ReferencePageTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.ReferencePageTableLayoutPanel.Name = "ReferencePageTableLayoutPanel"
            Me.ReferencePageTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.ReferencePageTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.ReferencePageTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'referenceButtonsTableLayoutPanel
            '
            resources.ApplyResources(Me.referenceButtonsTableLayoutPanel, "referenceButtonsTableLayoutPanel")
            Me.referenceButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.referenceButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.referenceButtonsTableLayoutPanel.Controls.Add(Me.UnusedReferences, 0, 0)
            Me.referenceButtonsTableLayoutPanel.Controls.Add(Me.ReferencePathsButton, 1, 0)
            Me.referenceButtonsTableLayoutPanel.Margin = New System.Windows.Forms.Padding(3, 0, 0, 3)
            Me.referenceButtonsTableLayoutPanel.Name = "referenceButtonsTableLayoutPanel"
            Me.referenceButtonsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'UnusedReferences
            '
            'UnusedReferences has been broken for some time now and no one has reported its failure.
            'Hence we are no longer supporting it. Instead of removing the associated code we are 
            'making the button invisible.
            resources.ApplyResources(Me.UnusedReferences, "UnusedReferences")
            Me.UnusedReferences.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UnusedReferences.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.UnusedReferences.Name = "UnusedReferences"
            Me.UnusedReferences.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            Me.UnusedReferences.Visible = False
            '
            'ReferencePathsButton
            '
            resources.ApplyResources(Me.ReferencePathsButton, "ReferencePathsButton")
            Me.ReferencePathsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.ReferencePathsButton.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
            Me.ReferencePathsButton.Name = "ReferencePathsButton"
            Me.ReferencePathsButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'addRemoveButtonsTableLayoutPanel
            '
            resources.ApplyResources(Me.addRemoveButtonsTableLayoutPanel, "addRemoveButtonsTableLayoutPanel")
            Me.ReferencePageTableLayoutPanel.SetColumnSpan(Me.addRemoveButtonsTableLayoutPanel, 2)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.addRemoveButtonsTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.addRemoveButtonsTableLayoutPanel.Controls.Add(Me.RemoveReference, 1, 0)
            Me.addRemoveButtonsTableLayoutPanel.Controls.Add(Me.addSplitButton, 0, 0)
            Me.addRemoveButtonsTableLayoutPanel.Controls.Add(Me.UpdateReferences, 2, 0)
            Me.addRemoveButtonsTableLayoutPanel.Margin = New System.Windows.Forms.Padding(3, 3, 0, 3)
            Me.addRemoveButtonsTableLayoutPanel.Name = "addRemoveButtonsTableLayoutPanel"
            Me.addRemoveButtonsTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'RemoveReference
            '
            resources.ApplyResources(Me.RemoveReference, "RemoveReference")
            Me.RemoveReference.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.RemoveReference.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
            Me.RemoveReference.Name = "RemoveReference"
            Me.RemoveReference.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'addSplitButton
            '
            resources.ApplyResources(Me.addSplitButton, "addSplitButton")
            Me.addSplitButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.addSplitButton.ContextMenuStrip = Me.addContextMenuStrip
            Me.addSplitButton.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.addSplitButton.Name = "addSplitButton"
            Me.addSplitButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'addContextMenuStrip
            '
            resources.ApplyResources(Me.addContextMenuStrip, "addContextMenuStrip")
            Me.addContextMenuStrip.GripMargin = New System.Windows.Forms.Padding(2)
            Me.addContextMenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.referenceToolStripMenuItem, Me.serviceReferenceToolStripMenuItem, Me.webReferenceToolStripMenuItem})
            Me.addContextMenuStrip.Name = "addContextMenuStrip"
            '
            'referenceToolStripMenuItem
            '
            Me.referenceToolStripMenuItem.Name = "referenceToolStripMenuItem"
            resources.ApplyResources(Me.referenceToolStripMenuItem, "referenceToolStripMenuItem")
            '
            'webReferenceToolStripMenuItem
            '
            Me.webReferenceToolStripMenuItem.Name = "webReferenceToolStripMenuItem"
            resources.ApplyResources(Me.webReferenceToolStripMenuItem, "webReferenceToolStripMenuItem")
            '
            'serviceReferenceToolStripMenuItem
            '
            Me.serviceReferenceToolStripMenuItem.Name = "serviceReferenceToolStripMenuItem"
            resources.ApplyResources(Me.serviceReferenceToolStripMenuItem, "serviceReferenceToolStripMenuItem")
            '
            'UpdateReferences
            '
            resources.ApplyResources(Me.UpdateReferences, "UpdateReferences")
            Me.UpdateReferences.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UpdateReferences.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
            Me.UpdateReferences.Name = "UpdateReferences"
            Me.UpdateReferences.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'ImportsListLabel
            '
            resources.ApplyResources(Me.ImportsListLabel, "ImportsListLabel")
            Me.addUserImportTableLayoutPanel.SetColumnSpan(Me.ImportsListLabel, 4)
            Me.ImportsListLabel.Margin = New System.Windows.Forms.Padding(0, 3, 3, 0)
            Me.ImportsListLabel.Name = "ImportsListLabel"
            '
            'AddUserImportButton
            '
            resources.ApplyResources(Me.AddUserImportButton, "AddUserImportButton")
            Me.AddUserImportButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.AddUserImportButton.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
            Me.AddUserImportButton.Name = "AddUserImportButton"
            Me.AddUserImportButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'UserImportTextBox
            '
            resources.ApplyResources(Me.UserImportTextBox, "UserImportTextBox")
            Me.addUserImportTableLayoutPanel.SetColumnSpan(Me.UserImportTextBox, 2)
            Me.UserImportTextBox.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.UserImportTextBox.Name = "UserImportTextBox"
            '
            'ImportList
            '
            resources.ApplyResources(Me.ImportList, "ImportList")
            Me.addUserImportTableLayoutPanel.SetColumnSpan(Me.ImportList, 3)
            Me.ImportList.FormattingEnabled = True
            Me.ImportList.Margin = New System.Windows.Forms.Padding(0, 3, 3, 3)
            Me.ImportList.Name = "ImportList"
            Me.ImportList.SelectionMode = System.Windows.Forms.SelectionMode.One
            '
            'addUserImportTableLayoutPanel
            '
            resources.ApplyResources(Me.addUserImportTableLayoutPanel, "addUserImportTableLayoutPanel")
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addUserImportTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.AddUserImportButton, 2, 1)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.UpdateUserImportButton, 3, 2)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.ImportsListLabel, 0, 0)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.ImportList, 0, 2)
            Me.addUserImportTableLayoutPanel.Controls.Add(Me.UserImportTextBox, 0, 1)
            Me.addUserImportTableLayoutPanel.Name = "addUserImportTableLayoutPanel"
            Me.addUserImportTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.addUserImportTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.addUserImportTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            '
            'UpdateUserImportButton
            '
            resources.ApplyResources(Me.UpdateUserImportButton, "UpdateUserImportButton")
            Me.UpdateUserImportButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UpdateUserImportButton.Name = "UpdateUserImportButton"
            Me.UpdateUserImportButton.Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)
            '
            'ReferencePageSplitContainer
            '
            Me.ReferencePageSplitContainer.BackColor = System.Drawing.SystemColors.Control
            Me.ReferencePageSplitContainer.AccessibleRole = System.Windows.Forms.AccessibleRole.Separator
            resources.ApplyResources(Me.ReferencePageSplitContainer, "ReferencePageSplitContainer")
            Me.ReferencePageSplitContainer.Name = "ReferencePageSplitContainer"
            '
            'ReferencePageSplitContainer.Panel1
            '
            Me.ReferencePageSplitContainer.Panel1.BackColor = System.Drawing.SystemColors.Control
            Me.ReferencePageSplitContainer.Panel1.Controls.Add(Me.ReferencePageTableLayoutPanel)
            '
            'ReferencePageSplitContainer.Panel2
            '
            Me.ReferencePageSplitContainer.Panel2.BackColor = System.Drawing.SystemColors.Control
            Me.ReferencePageSplitContainer.Panel2.Controls.Add(Me.addUserImportTableLayoutPanel)
            '
            'ReferencePropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.BackColor = System.Drawing.SystemColors.Control
            Me.Controls.Add(Me.ReferencePageSplitContainer)
            Me.MinimumSize = New System.Drawing.Size(538, 480)
            Me.Name = "ReferencePropPage"
            Me.ReferencePageSplitContainer.Panel1MinSize = 160
            Me.ReferencePageSplitContainer.Panel2MinSize = 160
            Me.ReferencePageTableLayoutPanel.ResumeLayout(False)
            Me.ReferencePageTableLayoutPanel.PerformLayout()
            Me.referenceButtonsTableLayoutPanel.ResumeLayout(False)
            Me.referenceButtonsTableLayoutPanel.PerformLayout()
            Me.addRemoveButtonsTableLayoutPanel.ResumeLayout(False)
            Me.addRemoveButtonsTableLayoutPanel.PerformLayout()
            Me.addContextMenuStrip.ResumeLayout(False)
            Me.addUserImportTableLayoutPanel.ResumeLayout(False)
            Me.addUserImportTableLayoutPanel.PerformLayout()
            Me.ReferencePageSplitContainer.Panel1.ResumeLayout(False)
            Me.ReferencePageSplitContainer.Panel1.PerformLayout()
            Me.ReferencePageSplitContainer.Panel2.ResumeLayout(False)
            Me.ReferencePageSplitContainer.Panel2.PerformLayout()
            Me.ReferencePageSplitContainer.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub

    End Class

End Namespace
