' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    '''  BaseRootDesigner can be used as a base class for other RootDesigner. It handles
    '''  -   Menu commands
    ''' </summary>
    ''' <remarks>
    ''' To hooks up menu items (including tool box, context menu, etc...)
    ''' -----------------------------------------------------------------
    ''' 1.    Define the menus in designerui directory
    '''       (see VSIP documents | Advanced VSPackage Support | Implementing Menu and Toolbar Commands).
    '''       -   Add some unique ID for your menu, group and command into VisualStudioEditorsID.h.
    '''       -   Add those groups, menus and commands into Menus.ctc file (see Command Table Format).
    ''' 2.    Define those constants in vbpackage\Constants.vb - MenuConstants.
    '''       Only expose the final CommandID (combination of the GUID and the command ID).
    ''' 3.    BaseRootDesigner exposes utilities methods to allow you registering menus with the shells, 
    '''       and showing context menus. 
    '''       a. For each command menu:
    '''           - Defines an EventHandler to handle the invoke of that command.
    '''           - Optional: defines functions to check if the command is Enabled or Checked.
    '''           - Defines a DesignerMenuCommand for that command.
    '''       b. Register the commands using BaseRootDesigner.RegisterMenuCommands.
    ''' 4.    In case it is a context menu, use BaseRootDesigner.ShowContextMenu to show it from the designer view.
    ''' </remarks>
    Friend MustInherit Class BaseRootDesigner
        Inherits ComponentDesigner
        Implements IServiceProvider

        '= PUBLIC =============================================================
        ';Methods
        '==========

        Protected Overrides Sub Dispose(Disposing As Boolean)
            If Disposing Then
                RemoveMenuCommands()
            End If

            MyBase.Dispose(Disposing)
        End Sub

        '= FRIEND =============================================================

        ''' <summary>
        '''  Exposes GetService from ComponentDesigner to other classes in this assembly to get a service.
        ''' </summary>
        ''' <param name="ServiceType">The type of the service being asked for.</param>
        ''' <returns>The requested service, if it exists.</returns>
        Friend Shadows Function GetService(ServiceType As Type) As Object Implements IServiceProvider.GetService
            Return MyBase.GetService(ServiceType)
        End Function

        ''' <summary>
        '''  Returns a cached ISelectionService.
        ''' </summary>
        ''' <value>The cached ISelectionService.</value>
        Friend ReadOnly Property SelectionService As ISelectionService
            Get
                If _selectionService Is Nothing Then
                    SyncLock _syncLockObject
                        If _selectionService Is Nothing Then
                            _selectionService = CType(MyBase.GetService(GetType(ISelectionService)), ISelectionService)
                            Debug.Assert(_selectionService IsNot Nothing, "Cannot get ISelectionService!!!")
                        End If
                    End SyncLock
                End If
                Return _selectionService
            End Get
        End Property

        ''' <summary>
        '''   Registers a list of menu commands to the shell, also registers common menu commands
        '''   owned by the BaseRootDesigner if specified.
        ''' </summary>
        ''' <param name="MenuCommands">An array list containing the menu commands to add.</param>
        ''' <param name="KeepRegisteredMenuCommands">
        '''  TRUE to keep previously registered menu commands for this designer.
        '''  FALSE otherwise, the root designer will clear its menu commands list and add the new one.
        ''' </param>
        ''' <remarks>Child root designers call this method to register their own menu commands. 
        '''      See ResourceEditorRootDesigner.</remarks>
        Friend Sub RegisterMenuCommands(MenuCommands As ArrayList,
                Optional KeepRegisteredMenuCommands As Boolean = True)
            ' Clear the list of menu commands if specified.
            If Not KeepRegisteredMenuCommands Then
                For Each MenuCommand As MenuCommand In Me.MenuCommands
                    MenuCommandService.RemoveCommand(MenuCommand)
                Next
                Me.MenuCommands.Clear()
            End If

            ' Register the new ones
            For Each MenuCommand As MenuCommand In MenuCommands
                MenuCommandService.AddCommand(MenuCommand)
                Me.MenuCommands.Add(MenuCommand)
            Next
        End Sub

        ''' <summary>
        ''' See <see cref="RegisterMenuCommands(ArrayList, Boolean)"/>.
        ''' </summary>
        ''' <remarks>This just converts <paramref name="MenuCommands"/> to an <see cref="ArrayList"/> and passes it
        ''' to <see cref="RegisterMenuCommands(ArrayList, Boolean)"/>. Once we're done refactoring this class to
        ''' eliminate <see cref="ArrayList"/> that overload will go away.</remarks>
        Friend Sub RegisterMenuCommands(MenuCommands As List(Of MenuCommand),
                Optional KeepRegisteredMenuCommands As Boolean = True)
            RegisterMenuCommands(New ArrayList(MenuCommands), KeepRegisteredMenuCommands)
        End Sub

        Friend Sub RemoveMenuCommands()
            'Iterate backwards to avoid problems removing while iterating
            For i As Integer = MenuCommands.Count - 1 To 0 Step -1
                Dim MenuCommand As MenuCommand = DirectCast(MenuCommands(i), MenuCommand)
                MenuCommandService.RemoveCommand(MenuCommand)
                MenuCommands.RemoveAt(i)
            Next
            Debug.Assert(MenuCommands.Count = 0)
        End Sub

        ''' <summary>
        ''' Shows the specified context menu at the specified position.
        ''' </summary>
        ''' <param name="ContextMenuID">The id of the context menu, usually from Constants.MenuConstants.</param>
        ''' <param name="X">The X coordinate to show the context menu.</param>
        ''' <param name="Y">The Y coordinate to show the context menu.</param>
        ''' <remarks>We don't expose the menu command service so other classes would not call 
        '''      AddCommand, RemoveCommand, etc... easily.</remarks>
        Friend Sub ShowContextMenu(ContextMenuID As CommandID, X As Integer, Y As Integer)
            MenuCommandService.ShowContextMenu(ContextMenuID, X, Y)
        End Sub

        ''' <summary>
        '''  Refreshes the status of all the menus of the current designer. 
        '''  This is called from DesignerMenuCommand after each invoke.
        ''' </summary>
        Friend Sub RefreshMenuStatus()
            For Each MenuItem As MenuCommand In MenuCommands
                Debug.Assert(MenuItem IsNot Nothing, "MenuItem IsNot Nothing!")
                Dim designerMenuCommand = TryCast(MenuItem, DesignerMenuCommand)
                If designerMenuCommand IsNot Nothing Then
                    designerMenuCommand.RefreshStatus()
                End If
            Next
        End Sub

        '= PROTECTED ==========================================================

        '= PRIVATE ============================================================

        ''' <summary>
        '''  Returns the menu command service that allows adding, removing, finding command 
        '''  as well as showing context menu.
        ''' </summary>
        ''' <value>The IMenuCommandService from the shell.</value>
        ''' <remarks>Don't want to expose this one to other classes to encourage using RegisterMenuCommands.</remarks>
        Private ReadOnly Property MenuCommandService As IMenuCommandService
            Get
                If _menuCommandService Is Nothing Then
                    SyncLock _syncLockObject
                        If _menuCommandService Is Nothing Then
                            _menuCommandService = CType(GetService(GetType(IMenuCommandService)), IMenuCommandService)
                            Debug.Assert(_menuCommandService IsNot Nothing, "Cannot get menu command service!")
                        End If
                    End SyncLock
                End If
                Return _menuCommandService
            End Get
        End Property

        ''' <summary>
        '''  Returns an arraylist containing all the current registered commands from this designer.
        ''' </summary>
        ''' <value>An ArrayList contains MenuCommand.</value>
        Private ReadOnly Property MenuCommands As ArrayList
            Get
                Return _menuCommands
            End Get
        End Property

        ' All the menu commands this designer exposes. Use MenuCommands to access this.
        Private ReadOnly _menuCommands As New ArrayList
        ' Pointer to the IMenuCommandService.
        Private _menuCommandService As IMenuCommandService
        ' Pointer to ISelectionService
        Private _selectionService As ISelectionService
        ' SyncLock object used to lazy initialized private fields.
        Private ReadOnly _syncLockObject As New Object

    End Class
End Namespace
