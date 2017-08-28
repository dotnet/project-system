' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#Const WINFORMEDITOR = False ' Set to True to open in WinForm Editor. Remember to set it back.

Option Strict On
Option Explicit On
Imports System.ComponentModel.Design
Imports System.Windows.Forms
#If Not WINFORMEDITOR Then
Imports Microsoft.VisualStudio.Editors.Common
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

            Dim vsMenuService As IMenuCommandService = _
                TryCast( _
                MyExtensibilitySolutionService.Instance.GetService(GetType(IMenuCommandService)), _
                IMenuCommandService)
            Debug.Assert(vsMenuService IsNot Nothing, "Could not get vsMenuService!")
            listViewExtensions.MenuCommandService = vsMenuService

            RefreshExtensionsList()

            ' Resize each columns based on its content.
            If listViewExtensions.Items.Count > 0 Then
                For i As Integer = 0 To listViewExtensions.Columns.Count - 1
                    listViewExtensions.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent)
                Next
            End If
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

            ' Support sorting.
            _comparer = New ListViewComparer()
            _comparer.SortColumn = 0
            _comparer.Sorting = SortOrder.Ascending
            listViewExtensions.ListViewItemSorter = _comparer
            listViewExtensions.Sorting = SortOrder.Ascending

            'Opt out of page scaling since we're using AutoScaleMode
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

        Private Sub listViewExtensions_AddExtension(sender As Object, e As EventArgs) _
                Handles listViewExtensions.AddExtension
            AddExtension()
        End Sub

        Private Sub listViewExtensions_ColumnClick(sender As Object, e As ColumnClickEventArgs) _
                Handles listViewExtensions.ColumnClick
            ListViewComparer.HandleColumnClick(listViewExtensions, _comparer, e)
        End Sub

        Private Sub listViewExtensions_RemoveExtension(sender As Object, e As EventArgs) _
                Handles listViewExtensions.RemoveExtension
            RemoveExtension()
        End Sub

        Private Sub listViewExtensions_SelectedIndexChanged(sender As Object, e As EventArgs) _
                Handles listViewExtensions.SelectedIndexChanged
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
        ''' Enable / disalbe buttonRemove depending on the selected items in the list view.
        ''' </summary>
        Private Sub EnableButtonRemove()
            buttonRemove.Enabled = listViewExtensions.SelectedItems.Count > 0
        End Sub

        ''' ;ExtensionProjectItemGroupToListViewItem
        ''' <summary>
        ''' Return the ListViewItem for the given extension code file.
        ''' </summary>
        Private Function ExtensionProjectItemGroupToListViewItem(extensionProjectFile As MyExtensionProjectItemGroup) _
                As ListViewItem
            Debug.Assert(extensionProjectFile IsNot Nothing)

            Dim listItem As New ListViewItem(extensionProjectFile.DisplayName)
            listItem.Tag = extensionProjectFile
            listItem.SubItems.Add(extensionProjectFile.ExtensionVersion.ToString())
            listItem.SubItems.Add(extensionProjectFile.ExtensionDescription)

            Return listItem
        End Function

        ''' ;RefreshExtensionsList
        ''' <summary>
        ''' Refresh the extensions list view.
        ''' </summary>
        Private Sub RefreshExtensionsList()
            listViewExtensions.Items.Clear()
            Dim extProjItemGroups As List(Of MyExtensionProjectItemGroup) = _
                _projectService.GetExtensionProjectItemGroups()
            If extProjItemGroups IsNot Nothing Then
                For Each extProjItemGroup As MyExtensionProjectItemGroup In extProjItemGroups
                    listViewExtensions.Items.Add(ExtensionProjectItemGroupToListViewItem(extProjItemGroup))
                Next
                listViewExtensions.Sort()
            End If
            EnableButtonRemove()
        End Sub

        ''' ;RemoveExtension
        ''' <summary>
        ''' Remove the selected extensions.
        ''' </summary>
        Private Sub RemoveExtension()
            Debug.Assert(listViewExtensions.SelectedItems.Count > 0)

            Dim extProjItemGroups As New List(Of MyExtensionProjectItemGroup)
            For Each item As ListViewItem In listViewExtensions.SelectedItems
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
        Private _comparer As ListViewComparer
#End If

#Region "Windows Form Designer generated code"
        Friend WithEvents labelDescription As Label
        Friend WithEvents linkLabelHelp As VSThemedLinkLabel
        Friend WithEvents listViewExtensions As MyExtensionListView
        Friend WithEvents colHeaderExtensionName As ColumnHeader
        Friend WithEvents tableLayoutAddRemoveButtons As TableLayoutPanel
        Friend WithEvents buttonRemove As Button
        Friend WithEvents buttonAdd As Button
        Private _components As System.ComponentModel.IContainer
        Friend WithEvents colHeaderExtensionVersion As ColumnHeader
        Friend WithEvents colHeaderExtensionDescription As ColumnHeader

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MyExtensibilityPropPage))
            tableLayoutOverarching = New TableLayoutPanel
            labelDescription = New Label
            linkLabelHelp = New VSThemedLinkLabel
            listViewExtensions = New MyExtensionListView
            colHeaderExtensionName = New ColumnHeader
            colHeaderExtensionVersion = New ColumnHeader
            colHeaderExtensionDescription = New ColumnHeader
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
            tableLayoutOverarching.Controls.Add(listViewExtensions, 0, 2)
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
            'listViewExtensions
            '
            listViewExtensions.AutoArrange = False
            listViewExtensions.Columns.AddRange(New ColumnHeader() {colHeaderExtensionName, colHeaderExtensionVersion, colHeaderExtensionDescription})
            resources.ApplyResources(listViewExtensions, "listViewExtensions")
            listViewExtensions.FullRowSelect = True
            listViewExtensions.HideSelection = False
            listViewExtensions.Name = "listViewExtensions"
            listViewExtensions.ShowItemToolTips = True
            listViewExtensions.UseCompatibleStateImageBehavior = False
            listViewExtensions.View = View.Details
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
