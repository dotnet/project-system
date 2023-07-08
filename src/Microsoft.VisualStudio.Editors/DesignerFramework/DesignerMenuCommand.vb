' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

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
    Friend Class DesignerMenuCommand
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

        '= FRIEND =============================================================
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
        '   CommandHadler: The event handler to handle this menu item.
        '   CommandEnabledHandler: The event handler to check if this menu item should be enabled or not.
        '   CommandCheckedHandler: The event handler to check if this menu item should be checked or not.
        '   CommandVisibleHandler: The event handler to check if this menu item should be visible or not.
        '   AlwaysCheckStatus: True to always call the handlers to check for status. False to only call when the status
        '       is marked invalid.
        '   CommandText: If specified (and the TEXTMENUCHANGES flag is set for the command in the CTC file) you can 
        '       supplies your own text for the command. 
        '**************************************************************************
        Friend Sub New(RootDesigner As BaseRootDesigner, CommandID As CommandID,
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
        Friend Sub RefreshStatus()
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

    Friend Delegate Function CheckCommandStatusHandler(menuCommand As DesignerMenuCommand) As Boolean

    ''' <summary>
    ''' A combobox control on a MSO command bar needs two commands, one to actually execute the command
    ''' and another to fill the combobox with items. This is a helper class that you can register with 
    ''' the OleMenuCommandService in order to fill your combobox
    ''' </summary>
    Friend Class DesignerCommandBarComboBoxFiller
        Inherits DesignerMenuCommand

        Public Delegate Function ItemsGetter() As String()

        Private ReadOnly _getter As ItemsGetter

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="designer">Root designer associated with this command</param>
        ''' <param name="commandId">CommandID with GUID/id as specified for the command in the CTC file</param>
        ''' <param name="getter">Delegate that returns a list of strings to fill the combobox with</param>
        Public Sub New(designer As BaseRootDesigner, commandId As CommandID, getter As ItemsGetter)
            MyBase.New(designer, commandId, AddressOf CommandHandler)

            If getter Is Nothing Then
                Debug.Fail("You must specify a getter for this to work...")
                Throw New ArgumentNullException()
            End If
            Visible = True
            Enabled = True
            _getter = getter
        End Sub

        ''' <summary>
        ''' Mapping from the Exec to the getter delegate
        ''' </summary>
        ''' <param name="e"></param>
        Private Sub InstanceCommandHandler(e As Shell.OleMenuCmdEventArgs)
            If e Is Nothing Then
                Throw New ArgumentNullException
            End If

            If _getter IsNot Nothing Then
                Dim items As String() = _getter()
                Marshal.GetNativeVariantForObject(items, e.OutValue)
            End If
        End Sub

        ''' <summary>
        ''' Since we can't pass an instance method from our own class to our base's constructor, we 
        ''' have a shared method that forwards the call to the actual instance command handler...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Shared Sub CommandHandler(sender As Object, e As EventArgs)
            Dim oleEventArgs As Shell.OleMenuCmdEventArgs = TryCast(e, Shell.OleMenuCmdEventArgs)
            Dim cmdSender As DesignerCommandBarComboBoxFiller = TryCast(sender, DesignerCommandBarComboBoxFiller)
            If cmdSender Is Nothing OrElse oleEventArgs Is Nothing Then
                Throw New InvalidOperationException()
            End If
            cmdSender.InstanceCommandHandler(oleEventArgs)
        End Sub

    End Class

    ''' <summary>
    ''' MSO command bar combobox command helper
    ''' Will handle get/set of the current text in the combobox
    ''' </summary>
    ''' <remarks>
    ''' You also need to add an instance of a DesignerCommandBarComboBoxFiller in order to fill the 
    ''' combobox with items.... This class only handles the current selection!
    ''' </remarks>
    Friend Class DesignerCommandBarComboBox
        Inherits DesignerMenuCommand

        Public Delegate Function CurrentTextGetter() As String
        Public Delegate Sub CurrentTextSetter(value As String)

        Private ReadOnly _currentTextGetter As CurrentTextGetter
        Private ReadOnly _currentTextSetter As CurrentTextSetter

        ''' <summary>
        ''' Construct for the combobox command handler
        ''' </summary>
        ''' <param name="designer"></param>
        ''' <param name="commandId"></param>
        ''' <param name="currentTextGetter">Delegate to get the current text in the combobox</param>
        ''' <param name="currentTextSetter">Delegate to set the current text in the combobox</param>
        Public Sub New(designer As BaseRootDesigner, commandId As CommandID, currentTextGetter As CurrentTextGetter, currentTextSetter As CurrentTextSetter, enabledHandler As CheckCommandStatusHandler)
            MyBase.New(designer, commandId, AddressOf CommandHandler, enabledHandler)
            If currentTextGetter Is Nothing OrElse currentTextSetter Is Nothing Then
                Debug.Fail("You must specify a getter and setter method")
                Throw New ArgumentNullException()
            End If
            Visible = True
            Enabled = True
            _currentTextGetter = currentTextGetter
            _currentTextSetter = currentTextSetter
        End Sub

        ''' <summary>
        ''' Mapping from the Exec to the getter delegate
        ''' </summary>
        ''' <param name="e"></param>
        Private Sub InstanceCommandHandler(e As Shell.OleMenuCmdEventArgs)
            If e.InValue Is Nothing Then
                ' Request to get the current text...
                Marshal.GetNativeVariantForObject(_currentTextGetter(), e.OutValue)
            Else
                ' Request to set the text
                If TypeOf e.InValue IsNot String Then
                    Throw New InvalidOperationException()
                End If
                _currentTextSetter(DirectCast(e.InValue, String))
            End If
        End Sub

        ''' <summary>
        ''' Since we can't pass an instance method from our own class to our base's constructor, we 
        ''' have a shared method that forwards the call to the actual instance command handler...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Shared Sub CommandHandler(sender As Object, e As EventArgs)
            Dim oleEventArgs As Shell.OleMenuCmdEventArgs = TryCast(e, Shell.OleMenuCmdEventArgs)
            Dim cboSender As DesignerCommandBarComboBox = TryCast(sender, DesignerCommandBarComboBox)

            If oleEventArgs Is Nothing OrElse cboSender Is Nothing Then
                Throw New InvalidOperationException()
            End If

            cboSender.InstanceCommandHandler(oleEventArgs)
        End Sub

    End Class

    ''' <summary>    
    ''' This class is used to replace the command handlers when the designer surface is closed. When the
    ''' designer is opened again this imposter handler is being replaced with actual handlers.
    ''' </summary>
    ''' <remarks>
    ''' This handler acts as a place holder command handler when the actual handler which is bound to the
    ''' UI is deleted as the UI is closed.
    ''' </remarks>
    Friend Class DummyDesignerMenuCommand
        Inherits DesignerMenuCommand

        ''' <summary>
        ''' Constructs an instance of an DummyDesignerMenuCommand
        ''' </summary>
        ''' <param name="commandId">Id of the command.</param>
        ''' <remarks>Sets the command invisible and disabled.</remarks>
        Public Sub New(commandId As CommandID)
            MyBase.New(Nothing, commandId, AddressOf CommandHandler)
            Visible = False
            Enabled = False
        End Sub

        ''' <summary>        
        ''' Command handler of the command.
        ''' </summary>
        ''' <param name="sender">Sender of the event.</param>
        ''' <param name="e">Argument of the event.</param>
        ''' <remarks>This handler is never invoked since the command is disabled.</remarks>
        Private Shared Sub CommandHandler(sender As Object, e As EventArgs)
        End Sub
    End Class

    ''' <summary>
    ''' Helper class to handle a group of commands where only one should be checked (latched)
    ''' (similar to how radio buttons work)
    ''' </summary>
    Friend Class LatchedCommandGroup

        Private ReadOnly _commands As New Dictionary(Of Integer, MenuCommand)

        ''' <summary>
        ''' Add a command to the group
        ''' </summary>
        ''' <param name="Id">A unique (within the group) id of the command</param>
        ''' <param name="Command">The command to add</param>
        Public Sub Add(Id As Integer, Command As MenuCommand)
            _commands(Id) = Command
        End Sub

        ''' <summary>
        ''' Get the collection of commands that are in this group
        ''' </summary>
        Public ReadOnly Property Commands As ICollection
            Get
                Return _commands.Values
            End Get
        End Property

        ''' <summary>
        ''' Make the command passed in the only checked command in the group
        ''' </summary>
        ''' <param name="CommandToCheck"></param>
        ''' <remarks>Will uncheck all commands if the command passed in was not in the group...</remarks>
        Public Sub Check(CommandToCheck As MenuCommand)
            For Each Command As MenuCommand In _commands.Values
                If Command Is CommandToCheck Then
                    Command.Checked = True
                Else
                    Command.Checked = False
                End If
            Next
        End Sub

        ''' <summary>
        ''' Make the command associated with the given ID the only checked command in the group
        ''' </summary>
        ''' <param name="Id"></param>
        Public Sub Check(Id As Integer)
            Dim CommandToCheck As MenuCommand = Nothing
            If Not _commands.TryGetValue(Id, CommandToCheck) Then
                Throw New ArgumentOutOfRangeException
            End If
            Check(CommandToCheck)
        End Sub
    End Class
End Namespace
