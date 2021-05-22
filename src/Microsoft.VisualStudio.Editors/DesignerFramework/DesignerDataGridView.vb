' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' In case we're building an editor, the editor's view will contain some user controls built from FX.
    '''   These user controls handles context menu in a different way. To show the context menu the correct way,
    '''   we inherit from the FX's control and override their WndProc.
    '''   
    '''   DesignerDataGridView is our control inherited from DataGridView.
    ''' </summary>
    Friend Class DesignerDataGridView
        Inherits DataGridView

        ' ContextMenuShow will be raised when this list view needs to show its context menu.
        ' The derived control simply needs to handle this event to know when to show a
        '   context menu
        Public Event ContextMenuShow(sender As Object, e As MouseEventArgs)

        ' Give clients a chance to prevent the "edit on click" caused by some custom cell types
        Public Event CellClickBeginEdit(sender As Object, e As System.ComponentModel.CancelEventArgs)

        ' In the multiple selection mode, we shouldn't enter editMode automatically
        Private _inMultiSelectionMode As Boolean

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New()
            BackColor = SystemColors.Window
            ForeColor = SystemColors.WindowText
            ' Make sure the row headers have enough space to display the glyphs in HDPI
            RowHeadersWidth = DpiAwareness.LogicalToDeviceUnits(Handle, RowHeadersWidth)
        End Sub

        ''' <summary>
        ''' Return whether the DataGridView is in MultiSelection Mode...
        ''' </summary>
        Friend ReadOnly Property InMultiSelectionMode As Boolean
            Get
                Return _inMultiSelectionMode
            End Get
        End Property

        ''' <summary>
        ''' Indicate that we are about to begin edit due to a click on a cell.
        ''' </summary>
        ''' <param name="e"></param>
        Friend Overridable Sub OnCellClickBeginEdit(e As System.ComponentModel.CancelEventArgs)
            RaiseEvent CellClickBeginEdit(Me, e)
        End Sub

        ''' <summary>
        ''' We override Control.WndProc to raise the ContextMenuShow event.
        ''' </summary>
        ''' <param name="m">Windows message passed in by window.</param>
        ''' <remarks>Implementation based on sources\ndp\fx\src\WinForms\Managed\System\WinForms\Control.cs</remarks>
        Protected Overrides Sub WndProc(ByRef m As Message)
            ' We only handle the context menu specially.
            Select Case m.Msg
                Case Interop.Win32Constant.WM_CONTEXTMENU
                    Debug.WriteLineIf(Switches.DFContextMenu.TraceVerbose, "WM_CONTEXTMENU")
                    Dim EventArgs As MouseEventArgs = DesignUtil.GetContextMenuMouseEventArgs(m)
                    RaiseEvent ContextMenuShow(Me, EventArgs)
                Case Else
                    MyBase.WndProc(m)
            End Select
        End Sub

        ''' <summary>
        ''' We want to catch right mouse down so we can change the currently selected cell
        ''' before showing the context menu... 
        ''' </summary>
        ''' <param name="e"></param>
        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            Dim ht As HitTestInfo = HitTest(e.X, e.Y)

            If (ModifierKeys And (Keys.Control Or Keys.Shift)) <> 0 Then
                _inMultiSelectionMode = True
            End If

            Try

                ' If we are right-clicking on a cell that isn't selected, then we should
                ' clear the previous selection and select the new cell... (a'la Excel)
                If e.Button = System.Windows.Forms.MouseButtons.Right Then
                    Try
                        If ht.Type = DataGridViewHitTestType.Cell Then
                            ' Select new cell 
                            Dim clickedCell As DataGridViewCell = Rows(ht.RowIndex).Cells(ht.ColumnIndex)
                            If Not clickedCell.Selected Then
                                CurrentCell = clickedCell
                            End If
                        ElseIf ht.Type = DataGridViewHitTestType.RowHeader Then
                            If IsCurrentCellInEditMode _
                                OrElse (Not _inMultiSelectionMode AndAlso Not Rows(ht.RowIndex).Selected) _
                            Then
                                ' Clear the current cell so we make sure that we have validated it...
                                CurrentCell = Nothing
                                ClearSelection()
                            End If
                            Rows(ht.RowIndex).Selected = True
                        Else
                            ' We didn't click on any cell - let's select no cell. This makes sure that we commit
                            ' the current cell before we show the context menu
                            CurrentCell = Nothing
                        End If
                    Catch ex As InvalidOperationException
                        ' We don't really care if we failed to set the current cell... If we didn't
                        ' then it was probably a validation failure and we shouldn't show the context 
                        ' menu!
                    End Try
                End If

                MyBase.OnMouseDown(e)

                If IsCurrentCellInEditMode Then
                    Try
                        If e.Button = System.Windows.Forms.MouseButtons.Left Then
                            If ht.Type = DataGridViewHitTestType.None Then
                                ' Clear the current cell so we make sure that we have validated it...
                                CurrentCell = Nothing
                            ElseIf ht.Type = DataGridViewHitTestType.RowHeader Then
                                If ht.RowIndex = CurrentCell.RowIndex Then
                                    CurrentCell = Nothing
                                    Rows(ht.RowIndex).Selected = True
                                ElseIf Not IsCurrentCellDirty Then
                                    EndEdit(DataGridViewDataErrorContexts.CurrentCellChange)
                                End If
                            End If
                        End If
                    Catch ex As InvalidOperationException
                        ' We don't really care if we failed to set the current cell... If we didn't
                        ' then it was probably a validation failure and we shouldn't show the context 
                        ' menu!
                    End Try
                End If

                If Not IsCurrentCellInEditMode Then
                    If ht.Type = DataGridViewHitTestType.None Then
                        If Not _inMultiSelectionMode Then
                            ClearSelection()
                        End If
                    End If
                End If

            Finally
                _inMultiSelectionMode = False
            End Try
        End Sub

        ''' <summary>
        ''' We want to filter out Ctrl+0 for "our" datagridviews
        ''' (they normally do a clear cell, which is bad for comboboxcolumns...)
        ''' </summary>
        ''' <param name="m"></param>
        Protected Overrides Function ProcessKeyMessage(ByRef m As Message) As Boolean
            Dim ke As New KeyEventArgs(CType(CInt(m.WParam) Or ModifierKeys, Keys))

            If _
                ke.KeyCode = Keys.D0 _
                AndAlso ke.Control _
                AndAlso CurrentCell IsNot Nothing _
                AndAlso GetType(DataGridViewComboBoxCell).IsAssignableFrom(CurrentCell.GetType()) _
            Then
                Return True
            Else
                Return MyBase.ProcessKeyMessage(m)
            End If
        End Function

        Protected Overrides Function ProcessDataGridViewKey(e As KeyEventArgs) As Boolean

            ' SHIFT+Space usually selects entire row in DataGridView, prevent 
            ' that in it in edit mode so that it behaves like a usual text box
            If e.KeyCode = Keys.Space AndAlso e.Shift AndAlso IsCurrentCellInEditMode Then
                Return False
            Else
                Return MyBase.ProcessDataGridViewKey(e)
            End If

        End Function

        ''' <summary>
        ''' We want to filter out Ctrl+0 for "our" datagridviews
        ''' (they normally do a clear cell, which is bad for comboboxcolumns...)
        ''' </summary>
        ''' <param name="keyData"></param>
        <Security.Permissions.UIPermission(Security.Permissions.SecurityAction.LinkDemand, Window:=Security.Permissions.UIPermissionWindow.AllWindows)>
        Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
            Const CtrlD0 As Keys = Keys.D0 Or Keys.Control

            If (keyData And CtrlD0) = CtrlD0 Then
                Return False
            Else
                Return MyBase.ProcessDialogKey(keyData)
            End If
        End Function

#Region "Hybrid edit mode cell templates"

        ''' <summary>
        ''' Extremely simple cell template that enables a hybrid of EditCellOnF2OrKeyDown and EditOnEnter mode
        ''' </summary>
        Friend Class EditOnClickDataGridViewComboBoxCell
            Inherits DataGridViewComboBoxCell

            Protected Overrides Sub OnEnter(rowIndex As Integer, throughMouseClick As Boolean)
                Dim dfxdgv As DesignerDataGridView = TryCast(DataGridView, DesignerDataGridView)
                Dim e As New System.ComponentModel.CancelEventArgs(False)

                If dfxdgv IsNot Nothing Then
                    If dfxdgv.InMultiSelectionMode Then
                        MyBase.OnEnter(rowIndex, throughMouseClick)
                        Return
                    End If
                    dfxdgv.OnCellClickBeginEdit(e)
                End If

                If e.Cancel Then
                    ' The datagridview wanted us to use the "normal" select-on-click behavior                    
                    MyBase.OnEnter(rowIndex, throughMouseClick)
                Else
                    ' The default implementation sets a flag that indicates that the next mouse click should be 
                    ' ignored!
                    '
                    ' We don't do anything here, which means that it will get into edit mode on the next
                    ' mouse click :)
                End If
            End Sub

            Public Overrides Property [ReadOnly] As Boolean
                Get
                    Return MyBase.ReadOnly
                End Get
                Set
                    MyBase.ReadOnly = value
                    If value Then
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing
                    Else
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    End If
                End Set
            End Property
        End Class

        ''' <summary>
        ''' Extremely simple cell template that enables a hybrid of EditCellOnF2OrKeyDown and EditOnEnter mode
        ''' </summary>
        Friend Class EditOnClickDataGridViewTextBoxCell
            Inherits DataGridViewTextBoxCell

            Protected Overrides Sub OnEnter(rowIndex As Integer, throughMouseClick As Boolean)
                Dim dfxdgv As DesignerDataGridView = TryCast(DataGridView, DesignerDataGridView)
                Dim e As New System.ComponentModel.CancelEventArgs(False)

                If dfxdgv IsNot Nothing Then
                    If dfxdgv.InMultiSelectionMode Then
                        MyBase.OnEnter(rowIndex, throughMouseClick)
                        Return
                    End If
                    dfxdgv.OnCellClickBeginEdit(e)
                End If

                If e.Cancel Then
                    ' The datagridview wanted us to use the "normal" select-on-click behavior                    
                    MyBase.OnEnter(rowIndex, throughMouseClick)
                Else
                    ' The default implementation sets a flag that indicates that the next mouse click should be 
                    ' ignored!
                    '
                    ' We don't do anything here, which means that it will get into edit mode on the next
                    ' mouse click :)
                End If
            End Sub
        End Class

#End Region
    End Class

End Namespace
