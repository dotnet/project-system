' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.DesignerFramework

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensionListView
    ''' <summary>
    ''' List view used on My Extension Property Page which can show context menu
    ''' using IMenuCommandService.
    ''' </summary>
    Friend Class MyExtensionListView
        Inherits DesignerListView

        Public Event AddExtension(sender As Object, e As EventArgs)
        Public Event RemoveExtension(sender As Object, e As EventArgs)

        Public Property MenuCommandService() As IMenuCommandService
            Get
                Debug.Assert(_menuCommandService IsNot Nothing)
                Return _menuCommandService
            End Get
            Set(value As IMenuCommandService)
                Debug.Assert(value IsNot Nothing)
                UnregisterMenuCommands()
                _menuCommandService = value
                RegisterMenuCommands()
            End Set
        End Property

        Private Sub MyExtensionListView_ContextMenuShow( _
                sender As Object, e As MouseEventArgs) Handles Me.ContextMenuShow

            _menuCommandRemoveExtension.Enabled = SelectedItems.Count > 0

            MenuCommandService.ShowContextMenu( _
                Constants.MenuConstants.CommandIDMYEXTENSIONContextMenu, e.X, e.Y)
        End Sub

        Private Sub RegisterMenuCommands()
            For Each menuCommand As MenuCommand In _menuCommands
                Dim existingCommand As MenuCommand = MenuCommandService.FindCommand(menuCommand.CommandID)
                If existingCommand IsNot Nothing Then
                    MenuCommandService.RemoveCommand(existingCommand)
                End If
                MenuCommandService.AddCommand(menuCommand)
            Next
        End Sub

        Private Sub UnregisterMenuCommands()
            If _menuCommandService IsNot Nothing Then
                For Each menuCommand As MenuCommand In _menuCommands
                    _menuCommandService.RemoveCommand(menuCommand)
                Next
            End If
        End Sub

        Private Sub AddExtension_Click(sender As Object, e As EventArgs)
            RaiseEvent AddExtension(sender, e)
        End Sub

        Private Sub RemoveExtension_Click(sender As Object, e As EventArgs)
            RaiseEvent RemoveExtension(sender, e)
        End Sub

        Private _menuCommandService As IMenuCommandService

        Private _menuCommandAddExtension As New MenuCommand( _
            New EventHandler(AddressOf AddExtension_Click), _
            Constants.MenuConstants.CommandIDMyEXTENSIONAddExtension)
        Private _menuCommandRemoveExtension As New MenuCommand( _
            New EventHandler(AddressOf RemoveExtension_Click), _
            Constants.MenuConstants.CommandIDMyEXTENSIONRemoveExtension)
        Private _menuCommands() As MenuCommand = _
            {_menuCommandAddExtension, _menuCommandRemoveExtension}
    End Class
End Namespace