' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#Const WINFORMEDITOR = False ' Set to True to open in WinForm Editor. Remember to set it back.

Option Strict On
Option Explicit On
Imports System.ComponentModel.Design
Imports System.Windows.Forms

#If Not WINFORMEDITOR Then
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.MyExtensibility
#End If

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' ;MyExtensibilityPropPage
    ''' <summary>
    ''' Property pages for VB My namespace extensions in Application Designer.
    ''' </summary>
    ''' <remarks>
    ''' Initialization for a property page is done by overriding PreInit
    ''' </remarks>
    Friend Class MyExtensibilityPropPage
#If False Then ' Change to True to edit in designer
        Inherits UserControl

        Public Sub New()
            Me.InitializeComponent()
        End Sub
#Else
        Inherits PropPageUserControlBase

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PreInitPage()
            Debug.Assert(ProjectHierarchy IsNot Nothing)

            _projectService = MyExtensibilitySolutionService.Instance.GetProjectService(ProjectHierarchy)
            Debug.Assert(_projectService IsNot Nothing)

            Dim vsMenuService As IMenuCommandService =
                TryCast(
                MyExtensibilitySolutionService.Instance.GetService(GetType(IMenuCommandService)),
                IMenuCommandService)
            Debug.Assert(vsMenuService IsNot Nothing, "Could not get vsMenuService!")
            DataGridViewExtensions.MenuCommandService = vsMenuService

            RefreshExtensionsList()

            For Each column As DataGridViewColumn In DataGridViewExtensions.Columns
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            Next

        End Sub

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
        End Sub

        ''' <summary>
        ''' F1 support.
        ''' </summary>
        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpIDs.VBProjPropMyExtensions
        End Function

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            PageRequiresScaling = False

            linkLabelHelp.SetThemedColor(VsUIShell5Service, SupportsTheming)
        End Sub

#Region "Event handlers"

        Private Sub buttonAdd_Click(sender As Object, e As EventArgs) _
                Handles buttonAdd.Click
            AddExtension()
        End Sub

        Private Sub buttonRemove_Click(sender As Object, e As EventArgs) _
                Handles buttonRemove.Click
            RemoveExtension()
        End Sub

        Private Sub DataGridViewExtensions_AddExtension(sender As Object, e As EventArgs) _
                Handles DataGridViewExtensions.AddExtension
            AddExtension()
        End Sub

        Private Sub DataGridViewExtensions_RemoveExtension(sender As Object, e As EventArgs) _
                Handles DataGridViewExtensions.RemoveExtension
            RemoveExtension()
        End Sub

        Private Sub DataGridViewExtensions_SelectedIndexChanged(sender As Object, e As EventArgs) _
                Handles DataGridViewExtensions.SelectionChanged
            EnableButtonRemove()
        End Sub

        Private Sub OnProjectServiceExtensionChanged() Handles _projectService.ExtensionChanged
            RefreshExtensionsList()
        End Sub

        Private Sub OnLinkLabelHelpLinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles linkLabelHelp.LinkClicked
            DesignUtil.DisplayTopicFromF1Keyword(ServiceProvider, HelpIDs.Dlg_AddMyNamespaceExtensions)
        End Sub
#End Region

        ''' ;AddExtension
        ''' <summary>
        ''' Launch the Add extension dialog.
        ''' </summary>
        Private Sub AddExtension()
            Debug.Assert(_projectService IsNot Nothing)
            _projectService.AddExtensionsFromPropPage()
        End Sub

        ''' ;EnableButtonRemove
        ''' <summary>
        ''' Enable / disable buttonRemove depending on the selected items in the list view.
        ''' </summary>
        Private Sub EnableButtonRemove()
            buttonRemove.Enabled = DataGridViewExtensions.SelectedRows.Count > 0
        End Sub

        ''' ;ExtensionProjectItemGroupToListViewItem
        ''' <summary>
        ''' Return the ListViewItem for the given extension code file.
        ''' </summary>
        Private Shared Function ExtensionProjectItemGroupToDataGridViewRow(extensionProjectFile As MyExtensionProjectItemGroup) _
                As DataGridViewRow
            Debug.Assert(extensionProjectFile IsNot Nothing)

            Dim dataGridViewRow As New DataGridViewRow() With {
                .Tag = extensionProjectFile
            }

            dataGridViewRow.Cells.Add(New DataGridViewTextBoxCell() With {
                .Value = extensionProjectFile.DisplayName
            })
            dataGridViewRow.Cells.Add(New DataGridViewTextBoxCell() With {
                .Value = extensionProjectFile.ExtensionVersion.ToString()
            })
            dataGridViewRow.Cells.Add(New DataGridViewTextBoxCell() With {
                .Value = extensionProjectFile.ExtensionDescription
            })

            Return dataGridViewRow
        End Function

        ''' ;RefreshExtensionsList
        ''' <summary>
        ''' Refresh the extensions list view.
        ''' </summary>
        Private Sub RefreshExtensionsList()
            DataGridViewExtensions.Rows.Clear()
            Dim extProjItemGroups As List(Of MyExtensionProjectItemGroup) =
                _projectService.GetExtensionProjectItemGroups()
            If extProjItemGroups IsNot Nothing Then
                For Each extProjItemGroup As MyExtensionProjectItemGroup In extProjItemGroups
                    DataGridViewExtensions.Rows.Add(ExtensionProjectItemGroupToDataGridViewRow(extProjItemGroup))
                Next
            End If
            EnableButtonRemove()
        End Sub

        ''' ;RemoveExtension
        ''' <summary>
        ''' Remove the selected extensions.
        ''' </summary>
        Private Sub RemoveExtension()
            Debug.Assert(DataGridViewExtensions.SelectedRows.Count > 0)

            Dim extProjItemGroups As New List(Of MyExtensionProjectItemGroup)
            For Each item As DataGridViewRow In DataGridViewExtensions.SelectedRows
                Dim extProjItemGroup As MyExtensionProjectItemGroup = TryCast(item.Tag, MyExtensionProjectItemGroup)
                If extProjItemGroup IsNot Nothing Then
                    extProjItemGroups.Add(extProjItemGroup)
                End If
            Next

            Debug.Assert(_projectService IsNot Nothing)
            _projectService.RemoveExtensionsFromPropPage(extProjItemGroups)

            RefreshExtensionsList()
        End Sub

        Private WithEvents _projectService As MyExtensibilityProjectService = Nothing
#End If

#Region "Windows Form Designer generated code"
        Friend WithEvents labelDescription As Label
        Friend WithEvents linkLabelHelp As VSThemedLinkLabel
        Friend WithEvents DataGridViewExtensions As MyExtensionDataGridView
        Friend WithEvents colHeaderExtensionName As DataGridViewTextBoxColumn
        Friend WithEvents colHeaderExtensionVersion As DataGridViewTextBoxColumn
        Friend WithEvents colHeaderExtensionDescription As DataGridViewTextBoxColumn
        Friend WithEvents tableLayoutAddRemoveButtons As TableLayoutPanel
        Friend WithEvents buttonRemove As Button
        Friend WithEvents buttonAdd As Button

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MyExtensibilityPropPage))
            tableLayoutOverarching = New TableLayoutPanel
            labelDescription = New Label
            linkLabelHelp = New VSThemedLinkLabel
            DataGridViewExtensions = New MyExtensionDataGridView
            colHeaderExtensionName = New DataGridViewTextBoxColumn
            colHeaderExtensionVersion = New DataGridViewTextBoxColumn
            colHeaderExtensionDescription = New DataGridViewTextBoxColumn
            tableLayoutAddRemoveButtons = New TableLayoutPanel
            buttonRemove = New Button
            buttonAdd = New Button
            tableLayoutOverarching.SuspendLayout()
            tableLayoutAddRemoveButtons.SuspendLayout()
            SuspendLayout()
            '
            'tableLayoutOverarching
            '
            resources.ApplyResources(tableLayoutOverarching, "tableLayoutOverarching")
            tableLayoutOverarching.Controls.Add(labelDescription, 0, 0)
            tableLayoutOverarching.Controls.Add(linkLabelHelp, 0, 1)
            tableLayoutOverarching.Controls.Add(DataGridViewExtensions, 0, 2)
            tableLayoutOverarching.Controls.Add(tableLayoutAddRemoveButtons, 0, 3)
            tableLayoutOverarching.Name = "tableLayoutOverarching"
            '
            'labelDescription
            '
            resources.ApplyResources(labelDescription, "labelDescription")
            labelDescription.Name = "labelDescription"
            '
            'linkLabelHelp
            '
            resources.ApplyResources(linkLabelHelp, "linkLabelHelp")
            linkLabelHelp.Name = "linkLabelHelp"
            linkLabelHelp.TabStop = True
            '
            'DataGridViewExtensions
            '
            DataGridViewExtensions.Columns.AddRange(New DataGridViewTextBoxColumn() {colHeaderExtensionName, colHeaderExtensionVersion, colHeaderExtensionDescription})
            resources.ApplyResources(DataGridViewExtensions, "DataGridViewExtensions")
            DataGridViewExtensions.SelectionMode = DataGridViewSelectionMode.CellSelect
            DataGridViewExtensions.RowHeadersVisible = False

            DataGridViewExtensions.Name = "DataGridViewExtensions"

            DataGridViewExtensions.Dock = DockStyle.Fill
            DataGridViewExtensions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            DataGridViewExtensions.ReadOnly = True
            DataGridViewExtensions.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            DataGridViewExtensions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            DataGridViewExtensions.AllowUserToAddRows = False
            DataGridViewExtensions.CellBorderStyle = DataGridViewCellBorderStyle.None
            DataGridViewExtensions.BackgroundColor = Drawing.SystemColors.Window
            DataGridViewExtensions.AllowUserToResizeRows = False
            '
            'colHeaderExtensionName
            '
            resources.ApplyResources(colHeaderExtensionName, "colHeaderExtensionName")
            '
            'colHeaderExtensionVersion
            '
            resources.ApplyResources(colHeaderExtensionVersion, "colHeaderExtensionVersion")
            '
            'colHeaderExtensionDescription
            '
            resources.ApplyResources(colHeaderExtensionDescription, "colHeaderExtensionDescription")
            '
            'tableLayoutAddRemoveButtons
            '
            resources.ApplyResources(tableLayoutAddRemoveButtons, "tableLayoutAddRemoveButtons")
            tableLayoutAddRemoveButtons.Controls.Add(buttonRemove, 1, 0)
            tableLayoutAddRemoveButtons.Controls.Add(buttonAdd, 0, 0)
            tableLayoutAddRemoveButtons.Name = "tableLayoutAddRemoveButtons"
            '
            'buttonRemove
            '
            resources.ApplyResources(buttonRemove, "buttonRemove")
            buttonRemove.Name = "buttonRemove"
            buttonRemove.UseVisualStyleBackColor = True
            '
            'buttonAdd
            '
            resources.ApplyResources(buttonAdd, "buttonAdd")
            buttonAdd.Name = "buttonAdd"
            buttonAdd.UseVisualStyleBackColor = True
            '
            'MyExtensibilityPropPage
            '
            resources.ApplyResources(Me, "$this")
            AutoScaleMode = AutoScaleMode.Font
            Controls.Add(tableLayoutOverarching)
            Name = "MyExtensibilityPropPage"
            tableLayoutOverarching.ResumeLayout(False)
            tableLayoutOverarching.PerformLayout()
            tableLayoutAddRemoveButtons.ResumeLayout(False)
            tableLayoutAddRemoveButtons.PerformLayout()
            ResumeLayout(False)
            PerformLayout()

        End Sub
        Friend WithEvents tableLayoutOverarching As TableLayoutPanel
#End Region

    End Class
End Namespace
