' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.DesignerFramework

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Requests a new resource name from the user.
    ''' </summary>
    Friend NotInheritable Class DialogQueryName
        Inherits BaseDialog
        'Inherits System.Windows.Forms.Form

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        Public Sub New(ServiceProvider As IServiceProvider)
            MyBase.New(ServiceProvider)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            F1Keyword = HelpIDs.Dlg_QueryName
        End Sub

#Region " Windows Form Designer generated code "

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Friend WithEvents ButtonCancel As Button
        Friend WithEvents ButtonAdd As Button
        Friend WithEvents LabelDescription As Label
        Friend WithEvents TextBoxName As TextBox
        Friend WithEvents addCancelTableLayoutPanel As TableLayoutPanel
        Friend WithEvents overarchingTableLayoutPanel As TableLayoutPanel

        'Required by the Windows Form Designer
        Private ReadOnly _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(DialogQueryName))
            LabelDescription = New Label
            TextBoxName = New TextBox
            ButtonCancel = New Button
            ButtonAdd = New Button
            addCancelTableLayoutPanel = New TableLayoutPanel
            overarchingTableLayoutPanel = New TableLayoutPanel
            addCancelTableLayoutPanel.SuspendLayout()
            overarchingTableLayoutPanel.SuspendLayout()
            SuspendLayout()
            '
            'LabelDescription
            '
            resources.ApplyResources(LabelDescription, "LabelDescription")
            LabelDescription.Name = "LabelDescription"
            '
            'TextBoxName
            '
            resources.ApplyResources(TextBoxName, "TextBoxName")
            TextBoxName.Name = "TextBoxName"
            '
            'ButtonCancel
            '
            resources.ApplyResources(ButtonCancel, "ButtonCancel")
            ButtonCancel.DialogResult = DialogResult.Cancel
            ButtonCancel.Margin = New Padding(3, 0, 0, 0)
            ButtonCancel.Name = "ButtonCancel"
            '
            'ButtonAdd
            '
            resources.ApplyResources(ButtonAdd, "ButtonAdd")
            ButtonCancel.DialogResult = DialogResult.OK
            ButtonAdd.Margin = New Padding(0, 0, 3, 0)
            ButtonAdd.Name = "ButtonAdd"
            '
            'addCancelTableLayoutPanel
            '
            resources.ApplyResources(addCancelTableLayoutPanel, "addCancelTableLayoutPanel")
            addCancelTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
            addCancelTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
            addCancelTableLayoutPanel.Controls.Add(ButtonAdd, 0, 0)
            addCancelTableLayoutPanel.Controls.Add(ButtonCancel, 1, 0)
            addCancelTableLayoutPanel.Name = "addCancelTableLayoutPanel"
            addCancelTableLayoutPanel.RowStyles.Add(New RowStyle)
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            overarchingTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 280.0!))
            overarchingTableLayoutPanel.Controls.Add(LabelDescription, 0, 0)
            overarchingTableLayoutPanel.Controls.Add(addCancelTableLayoutPanel, 0, 2)
            overarchingTableLayoutPanel.Controls.Add(TextBoxName, 0, 1)
            overarchingTableLayoutPanel.Margin = New Padding(9)
            overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            overarchingTableLayoutPanel.RowStyles.Add(New RowStyle)
            overarchingTableLayoutPanel.RowStyles.Add(New RowStyle)
            overarchingTableLayoutPanel.RowStyles.Add(New RowStyle)
            '
            'DialogQueryName
            '
            resources.ApplyResources(Me, "$this")
            Controls.Add(overarchingTableLayoutPanel)
            FormBorderStyle = FormBorderStyle.FixedDialog
            Padding = New Padding(9, 9, 9, 0)
            HelpButton = True
            MaximizeBox = False
            MinimizeBox = False
            Name = "DialogQueryName"
            AutoScaleMode = AutoScaleMode.Font
            ShowIcon = False
            addCancelTableLayoutPanel.ResumeLayout(False)
            addCancelTableLayoutPanel.PerformLayout()
            overarchingTableLayoutPanel.ResumeLayout(False)
            overarchingTableLayoutPanel.PerformLayout()
            AcceptButton = ButtonAdd
            CancelButton = ButtonCancel
            ResumeLayout(False)
            PerformLayout()
        End Sub

#End Region

        'Set to true if the user cancels the dialog
        Private _canceled As Boolean

        ' RootDesigner
        Private _rootDesigner As ResourceEditorRootDesigner

        ''' <summary>
        ''' Requests a new resource name from the user.
        ''' </summary>
        ''' <param name="SuggestedName">The default name to show in the dialog when it is first shown.</param>
        ''' <param name="UserCancel">[Out] True iff the user canceled the dialog.</param>
        ''' <returns>The Name selected by the user.</returns>
        Public Shared Function QueryAddNewResourceName(RootDesigner As ResourceEditorRootDesigner, SuggestedName As String, ByRef UserCancel As Boolean) As String
            Dim Dialog As New DialogQueryName(RootDesigner)
            With Dialog
                Try
                    .TextBoxName.Text = SuggestedName
                    .ActiveControl = .TextBoxName
                    .TextBoxName.SelectionStart = 0
                    .TextBoxName.SelectionLength = .TextBoxName.Text.Length()
                    ._canceled = True
                    ._rootDesigner = RootDesigner
                    .ShowDialog()
                    If ._canceled Then
                        UserCancel = True
                        Return Nothing
                    Else
                        Return .TextBoxName.Text
                    End If
                Finally
                    .Dispose()
                End Try
            End With
        End Function

        ''' <summary>
        ''' Click handler for the Add button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ButtonAdd_Click(sender As Object, e As EventArgs) Handles ButtonAdd.Click
            Dim ResourceView As ResourceEditorView = _rootDesigner.GetView()
            Debug.Assert(ResourceView IsNot Nothing, "Why there is no view?")
            If ResourceView IsNot Nothing Then
                Dim NewResourceName As String = TextBoxName.Text
                Dim Exception As Exception = Nothing
                If String.IsNullOrEmpty(NewResourceName) Then
                    ResourceView.DsMsgBox(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_NameBlank, MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_NameBlank)
                ElseIf Not Resource.ValidateName(ResourceView.ResourceFile, NewResourceName, String.Empty, NewResourceName, Exception) Then
                    ResourceView.DsMsgBox(Exception)
                Else
                    _canceled = False
                    Close()
                End If

                'Set focus back to the textbox for the user to change the entry
                TextBoxName.Focus()
                TextBoxName.SelectionStart = 0
                TextBoxName.SelectionLength = TextBoxName.Text.Length
            End If
        End Sub

        ''' <summary>
        ''' Click handler for the Cancel button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ButtonCancel_Click(sender As Object, e As EventArgs) Handles ButtonCancel.Click
            Close()
        End Sub

        ''' <summary>
        ''' Click handler for the Help button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub DialogQueryName_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
    End Class

End Namespace
