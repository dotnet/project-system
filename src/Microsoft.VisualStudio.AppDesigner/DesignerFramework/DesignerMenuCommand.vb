' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework

    '**************************************************************************
    ';DesignerMenuCommand
    '
    'Remarks:
    '   This class is based on Microsoft.VSDesigner.DesignerFramework.DesignerMenuCommand.
    '       (wizard\vsdesigner\designer\microsoft\vsdesigner\DesignerFramework).
    '   It represents a shell menu, context menu, or tool box item.
    '   It inherits from System.ComponentModel.Design.MenuCommand and provides additional events
    '       to verify the status (checked, enabled) of the menu command at run-time.
    '   It also calls the root designer to refresh the status of all the menu commands 
    '       owned by the root designer after each Invoke.
    '**************************************************************************
    Public Class DesignerMenuCommand
        Inherits Shell.OleMenuCommand

        '= PUBLIC =============================================================
        ';Properties
        '==========

        '**************************************************************************
        ';OleStatus
        '
        'Summary:
        '   Gets the OLE command status code for this menu item.
        'Returns:
        '   An integer containing a mixture of status flags that reflect the state of this menu item.
        'Remarks:
        '   We also update the status of this menu item in this property based on 
        '   m_AlwaysCheckStatus and m_StatusValid flag.
        '**************************************************************************
        Public Overrides ReadOnly Property OleStatus As Integer
            Get
                If _alwaysCheckStatus OrElse Not _statusValid Then
                    UpdateStatus()
                End If
                Return MyBase.OleStatus
            End Get
        End Property

        ';Methods
        '==========

        '**************************************************************************
        ';Invoke
        '
        'Summary:
        '   Invokes the command.
        'Remarks:
        '   After invoking the command, we also call RefreshMenuStatus on the RootDesigner,
        '   which refreshes the status of all the menus the designer knows about.
        '**************************************************************************

        Public Overrides Sub Invoke()
            MyBase.Invoke()

            If _rootDesigner IsNot Nothing Then
                ' Refresh the status of all the menus for the current designer.
                _rootDesigner.RefreshMenuStatus()
            End If
        End Sub 'Invoke
        Public Overrides Sub Invoke(inArg As Object, outArg As IntPtr)
            MyBase.Invoke(inArg, outArg)

            If _rootDesigner IsNot Nothing Then
                ' Refresh the status of all the menus for the current designer.
                _rootDesigner.RefreshMenuStatus()
            End If
        End Sub

        Public Overrides Sub Invoke(inArg As Object)
            MyBase.Invoke(inArg)

            If _rootDesigner IsNot Nothing Then
                ' Refresh the status of all the menus for the current designer.
                _rootDesigner.RefreshMenuStatus()
            End If
        End Sub

        '= Public =============================================================
        ';Constructors
        '==========

        '**************************************************************************
        ';New
        '
        'Summary:
        '   Constructs a new designer menu item.
        'Params:
        '   RootDesigner: The root designer that owns this menu item (may be Nothing)
        '   CommandID: The command ID of this item. It comes from Constants.MenuConstants (and its value must match
        '       one of the constants in designerui\VisualStudioEditorsUI.h).
        '   CommandHandler: The event handler to handle this menu item.
        '   CommandEnabledHandler: The event handler to check if this menu item should be enabled or not.
        '   CommandCheckedHandler: The event handler to check if this menu item should be checked or not.
        '   CommandVisibleHandler: The event handler to check if this menu item should be visible or not.
        '   AlwaysCheckStatus: True to always call the handlers to check for status. False to only call when the status
        '       is marked invalid.
        '   CommandText: If specified (and the TEXTMENUCHANGES flag is set for the command in the CTC file) you can 
        '       supplies your own text for the command. 
        '**************************************************************************
        Public Sub New(RootDesigner As BaseRootDesigner, CommandID As CommandID,
                        CommandHandler As EventHandler,
                        Optional CommandEnabledHandler As CheckCommandStatusHandler = Nothing,
                        Optional CommandCheckedHandler As CheckCommandStatusHandler = Nothing,
                        Optional CommandVisibleHandler As CheckCommandStatusHandler = Nothing,
                        Optional AlwaysCheckStatus As Boolean = False,
                        Optional CommandText As String = Nothing)

            MyBase.New(CommandHandler, CommandID)

            _rootDesigner = RootDesigner
            _commandEnabledHandler = CommandEnabledHandler
            _commandCheckedHandler = CommandCheckedHandler
            _commandVisibleHandler = CommandVisibleHandler
            _alwaysCheckStatus = AlwaysCheckStatus
            If CommandText <> "" Then
                Text = CommandText
            End If
            Visible = True
            Enabled = True

            RefreshStatus()
        End Sub 'New

        ';Methods
        '==========

        '**************************************************************************
        ';RefreshStatus
        '
        'Summary:
        '   Refresh the status of the command.
        '**************************************************************************
        Public Sub RefreshStatus()
            _statusValid = False
            OnCommandChanged(EventArgs.Empty)
        End Sub 'RefreshStatus

        '= PROTECTED ==========================================================

        '= PRIVATE ============================================================

        '**************************************************************************
        ';UpdateStatus
        '
        'Summary:
        '   Calls the command status handlers (if any) to set the status of the command.
        '**************************************************************************
        Private Sub UpdateStatus()
            If _commandEnabledHandler IsNot Nothing Then
                Enabled = _commandEnabledHandler(Me)
            End If
            If _commandCheckedHandler IsNot Nothing Then
                Checked = _commandCheckedHandler(Me)
            End If
            If _commandVisibleHandler IsNot Nothing Then
                Visible = _commandVisibleHandler(Me)
            End If
            _statusValid = True
        End Sub 'UpdateStatus

        Private ReadOnly _rootDesigner As BaseRootDesigner ' Pointer to the RootDesigner allowing refreshing all menu commands.
        Private ReadOnly _commandEnabledHandler As CheckCommandStatusHandler ' Handler to check if the command should be enabled.
        Private ReadOnly _commandCheckedHandler As CheckCommandStatusHandler ' Handler to check if the command should be checked.
        Private ReadOnly _commandVisibleHandler As CheckCommandStatusHandler ' Handler to check if the command should be hidden.
        Private ReadOnly _alwaysCheckStatus As Boolean ' True to always check the status of the command after every call. False otherwise.
        Private _statusValid As Boolean ' Whether the status of the command is still valid.
    End Class 'DesignerMenuCommand

    Public Delegate Function CheckCommandStatusHandler(menuCommand As DesignerMenuCommand) As Boolean

End Namespace
