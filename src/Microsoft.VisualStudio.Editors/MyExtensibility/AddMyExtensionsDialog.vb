' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.ComponentModel
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;AddMyExtensionsDialog
    ''' <summary>
    ''' Dialog for adding My namespace extensions to VB project.
    ''' </summary>
    ''' <remarks>
    ''' - To edit this in WinForms Designer: change #If False to #If True.
    ''' </remarks>
    Friend Class AddMyExtensionsDialog
#If False Then ' Change to True to edit this in WinForms Designer.
        Inherits System.Windows.Forms.Form

        Public Sub New()
            MyBase.New()
            Me.InitializeComponent()
        End Sub
#Else
        Inherits DesignerFramework.BaseDialog

        Private Sub New()
        End Sub

        ''' ;New
        ''' <summary>
        ''' Construct the dialog with the given service provider and extension templates list.
        ''' </summary>
        Public Sub New(serviceProvider As IServiceProvider,
                extensionTemplates As List(Of MyExtensionTemplate))
            MyBase.New(serviceProvider)
            InitializeComponent()

            F1Keyword = HelpIDs.Dlg_AddMyNamespaceExtensions

            _extensionTemplates = extensionTemplates

            If _extensionTemplates IsNot Nothing Then
                For Each extensionTemplate As MyExtensionTemplate In _extensionTemplates
                    listViewExtensions.Items.Add(ExtensionTemplateToListViewItem(extensionTemplate))
                Next
            End If

            _comparer = New ListViewComparer With {
                .SortColumn = 0,
                .Sorting = SortOrder.Ascending
            }
            listViewExtensions.ListViewItemSorter = _comparer
            listViewExtensions.Sorting = _comparer.Sorting
            listViewExtensions.Sort()

            EnableButtonOK()
        End Sub
#End If

        ''' ;ExtensionTemplatesToAdd
        ''' <summary>
        ''' The selected extension templates to add to the project.
        ''' </summary>
        Public ReadOnly Property ExtensionTemplatesToAdd As List(Of MyExtensionTemplate)
            Get
                Return _extensionTemplatesToAdd
            End Get
        End Property

#Region "Event handlers"
        Private Sub listViewExtensions_ColumnClick(sender As Object, e As ColumnClickEventArgs) _
                Handles listViewExtensions.ColumnClick
            ListViewComparer.HandleColumnClick(listViewExtensions, _comparer, e)
        End Sub

        Private Sub listViewExtensions_DoubleClick(sender As Object, e As EventArgs) _
                Handles listViewExtensions.DoubleClick
            AddExtensions()
        End Sub

        Private Sub listViewExtensions_SelectedIndexChanged(sender As Object, e As EventArgs) _
                Handles listViewExtensions.SelectedIndexChanged
            EnableButtonOK()
        End Sub

        Private Sub buttonOK_Click(sender As Object, e As EventArgs) _
                Handles buttonOK.Click
            Debug.Assert(listViewExtensions.SelectedItems.Count > 0)
            AddExtensions()
        End Sub

        ''' <summary>
        ''' Click handler for the Help button. DevDiv Bugs 69458.
        ''' </summary>
        Private Sub AddMyExtensionDialog_HelpButtonClicked(
                sender As Object, e As CancelEventArgs) _
                Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
#End Region

        ''' ;AddExtensions
        ''' <summary>
        ''' Put the selected extensions to ExtensionTemplatesToAdd, set DialogResult to OK
        ''' and close the dialog.
        ''' </summary>
        Private Sub AddExtensions()
            If listViewExtensions.SelectedItems.Count > 0 Then
                _extensionTemplatesToAdd = New List(Of MyExtensionTemplate)

                For Each item As ListViewItem In listViewExtensions.SelectedItems
                    Dim extensionTemplate As MyExtensionTemplate = TryCast(item.Tag, MyExtensionTemplate)
                    If extensionTemplate IsNot Nothing Then
                        _extensionTemplatesToAdd.Add(extensionTemplate)
                    End If
                Next

                DialogResult = DialogResult.OK
                Close()
            End If
        End Sub

        ''' ;EnableButtonOK
        ''' <summary>
        ''' Enable/disable buttonOK depending on the selected items on the list view.
        ''' </summary>
        Private Sub EnableButtonOK()
            buttonOK.Enabled = listViewExtensions.SelectedItems.Count > 0
        End Sub

        ''' ;ExtensionTemplateToListViewItem
        ''' <summary>
        ''' Return a ListViewItem for the given extension template.
        ''' </summary>
        Private Shared Function ExtensionTemplateToListViewItem(
                extensionTemplate As MyExtensionTemplate) As ListViewItem
            Debug.Assert(extensionTemplate IsNot Nothing, "extensionTemplate is NULL!")

            Dim item As New ListViewItem(extensionTemplate.DisplayName) With {
                .Tag = extensionTemplate
            }
            item.SubItems.Add(extensionTemplate.Version.ToString())
            item.SubItems.Add(extensionTemplate.Description)
            Return item
        End Function

        Private ReadOnly _extensionTemplates As List(Of MyExtensionTemplate)
        Private _extensionTemplatesToAdd As List(Of MyExtensionTemplate)
        Private ReadOnly _comparer As ListViewComparer

#Region "Windows Forms Designer generated code"

        Friend WithEvents listViewExtensions As ListView
        Friend WithEvents buttonCancel As Button
        Friend WithEvents buttonOK As Button
        Friend WithEvents tableLayoutOKCancelButtons As TableLayoutPanel
        Friend WithEvents colHeaderExtensionName As ColumnHeader
        Friend WithEvents colHeaderExtensionDescription As ColumnHeader
        Friend WithEvents colHeaderExtensionVersion As ColumnHeader
        Friend WithEvents tableLayoutOverarching As TableLayoutPanel

        Private Sub InitializeComponent()
            Dim resources As ComponentResourceManager = New ComponentResourceManager(GetType(AddMyExtensionsDialog))
            tableLayoutOverarching = New TableLayoutPanel
            tableLayoutOKCancelButtons = New TableLayoutPanel
            buttonOK = New Button
            buttonCancel = New Button
            listViewExtensions = New ListView
            colHeaderExtensionName = New ColumnHeader
            colHeaderExtensionVersion = New ColumnHeader
            colHeaderExtensionDescription = New ColumnHeader
            tableLayoutOverarching.SuspendLayout()
            tableLayoutOKCancelButtons.SuspendLayout()
            SuspendLayout()
            '
            'tableLayoutOverarching
            '
            resources.ApplyResources(tableLayoutOverarching, "tableLayoutOverarching")
            tableLayoutOverarching.Controls.Add(tableLayoutOKCancelButtons, 0, 1)
            tableLayoutOverarching.Controls.Add(listViewExtensions, 0, 0)
            tableLayoutOverarching.Name = "tableLayoutOverarching"
            '
            'tableLayoutOKCancelButtons
            '
            resources.ApplyResources(tableLayoutOKCancelButtons, "tableLayoutOKCancelButtons")
            tableLayoutOKCancelButtons.Controls.Add(buttonOK, 0, 0)
            tableLayoutOKCancelButtons.Controls.Add(buttonCancel, 1, 0)
            tableLayoutOKCancelButtons.Name = "tableLayoutOKCancelButtons"
            '
            'buttonOK
            '
            resources.ApplyResources(buttonOK, "buttonOK")
            buttonOK.Name = "buttonOK"
            buttonOK.UseVisualStyleBackColor = True
            '
            'buttonCancel
            '
            resources.ApplyResources(buttonCancel, "buttonCancel")
            buttonCancel.DialogResult = DialogResult.Cancel
            buttonCancel.Name = "buttonCancel"
            buttonCancel.UseVisualStyleBackColor = True
            '
            'listViewExtensions
            '
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
            'AddMyExtensionsDialog
            '
            AcceptButton = buttonOK
            CancelButton = buttonCancel
            resources.ApplyResources(Me, "$this")
            Controls.Add(tableLayoutOverarching)
            HelpButton = True
            MaximizeBox = False
            MinimizeBox = False
            Name = "AddMyExtensionsDialog"
            ShowIcon = False
            ShowInTaskbar = False
            tableLayoutOverarching.ResumeLayout(False)
            tableLayoutOverarching.PerformLayout()
            tableLayoutOKCancelButtons.ResumeLayout(False)
            tableLayoutOKCancelButtons.PerformLayout()
            ResumeLayout(False)

        End Sub

#End Region

    End Class

End Namespace
