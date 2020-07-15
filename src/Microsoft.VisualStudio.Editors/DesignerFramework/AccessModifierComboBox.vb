' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.ComponentModel
Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' Gets the language-dependent terminology for Public/Friend
    ''' </summary>
    Friend Class AccessModifierConverter
        Private ReadOnly _converter As TypeConverter

        Public Enum Access
            [Public]
            [Friend]
        End Enum

        Public Sub New(provider As CodeDomProvider)
            If provider IsNot Nothing Then
                Dim converter As TypeConverter = provider.GetConverter(GetType(MemberAttributes))

                'If the convert we got is just the standard converter, the codedom provider 
                '  must not support this converter.  We're better off using defaults.
                If converter.GetType() IsNot GetType(TypeConverter) OrElse Not converter.CanConvertTo(GetType(String)) Then
                    _converter = converter
                End If
            End If
        End Sub

        ''' <summary>
        ''' Gets the language-dependent terminology for Public/Friend
        ''' </summary>
        ''' <param name="accessibility"></param>
        Public Function ConvertToString(accessibility As Access) As String
            Select Case accessibility
                Case Access.Friend
                    If _converter IsNot Nothing Then
                        Return _converter.ConvertToString(MemberAttributes.Assembly)
                    Else
                        Return "Internal"
                    End If
                Case Access.Public
                    If _converter IsNot Nothing Then
                        Return _converter.ConvertToString(MemberAttributes.Public)
                    Else
                        Return "Public"
                    End If
                Case Else
                    Throw CreateArgumentException(NameOf(accessibility))
            End Select
        End Function
    End Class

    Friend MustInherit Class AccessModifierCombobox
        Implements IDisposable

        Private _isDisposed As Boolean
        Private ReadOnly _rootDesigner As BaseRootDesigner
        Private ReadOnly _projectItem As EnvDTE.ProjectItem
        Private ReadOnly _serviceProvider As IServiceProvider
        Private ReadOnly _namespaceToOverrideIfCustomToolIsEmpty As String
        Private ReadOnly _codeGeneratorEntries As New List(Of CodeGenerator)
        Private ReadOnly _recognizedCustomToolValues As New List(Of String)

        Private _designerCommandBarComboBoxCommand As DesignerCommandBarComboBox
        Private _commandIdCombobox As CommandID

        ' Cached flag to indicate if the custom tools associated with this combobox are
        ' registered for the current project type.
        ' The states are True (registered), False (not registered) or Missing (we haven't 
        ' checked the project system yet)
        ' This field should only be accessed through the CustomToolsRegistered property.
        Private _customToolsRegistered As Boolean?

        Public Enum Access
            [Public]
            [Friend]
        End Enum

#Region "Nested class CodeGenerator"

        Private MustInherit Class CodeGenerator
            Private ReadOnly _customToolValue As String

            Public Sub New(customToolValue As String)
                Requires.NotNull(customToolValue, NameOf(customToolValue))
                _customToolValue = customToolValue
            End Sub

            Public MustOverride ReadOnly Property DisplayName As String
            Public ReadOnly Property CustomToolValue As String
                Get
                    Return _customToolValue
                End Get
            End Property
        End Class

        Private Class CodeGeneratorWithName
            Inherits CodeGenerator

            Private ReadOnly _displayName As String

            Public Sub New(displayName As String, customToolValue As String)
                MyBase.New(customToolValue)

                Requires.NotNull(displayName, NameOf(displayName))
                _displayName = displayName
            End Sub

            Public Overrides ReadOnly Property DisplayName As String
                Get
                    Return _displayName
                End Get
            End Property
        End Class

        Private Class CodeGeneratorWithDelayedName
            Inherits CodeGenerator

            Private ReadOnly _accessibility As AccessModifierConverter.Access
            Private ReadOnly _serviceProvider As IServiceProvider

            Public Sub New(accessibility As AccessModifierConverter.Access, serviceProvider As IServiceProvider, customToolValue As String)
                MyBase.New(customToolValue)

                Requires.NotNull(serviceProvider, NameOf(serviceProvider))

                _accessibility = accessibility
                _serviceProvider = serviceProvider
            End Sub

            Public Overrides ReadOnly Property DisplayName As String
                Get
                    Dim codeDomProvider As CodeDomProvider = Nothing
                    Dim vsmdCodeDomProvider As IVSMDCodeDomProvider = TryCast(_serviceProvider.GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                    If vsmdCodeDomProvider IsNot Nothing Then
                        codeDomProvider = TryCast(vsmdCodeDomProvider.CodeDomProvider(), CodeDomProvider)
                    End If

                    Return New AccessModifierConverter(codeDomProvider).ConvertToString(_accessibility)
                End Get
            End Property
        End Class

#End Region

#Region "Nested class "

        ''' <summary>
        ''' This class registers/unregisters a DesignerMenuCommand with the package.
        ''' This is needed for the access modifier combobox because we want the
        '''   combobox to remain enabled when the user clicks away from the designer
        '''   and onto, say, the solution explorer.
        ''' We can get this effect as long as we don't use DefaultDisabled in the
        '''   .vsct file.  However, when the user clicks away from the editor, the
        '''   shell will keep the combobox enabled, but it will remove the selected
        '''   text, so it goes blank.  This all is confusing to the user.  Unfortunately,
        '''   command bars weren't really designed for editors (we're using them because
        '''   historical reasons forced on us by DTP).
        ''' To keep the text from going blank when a designer doesn't have the focus,
        '''   we need to register a command handler with our package.  This class keeps
        '''   track of the last command registered with the package for a given
        '''   commandID.
        ''' When a designer is activated, it should register its command here for the
        '''   access modifier combobox.  It should only unregister it when the designer
        '''   is closed.  The last designer to register here will control the case
        '''   when the designer is not focused and the command gets routed through 
        '''   the package.
        ''' </summary>
        Friend Class DesignerMenuCommandForwarder

            '
            ' Map from command ID to LIFO list of command handlers. The item at the head of the list is the item 
            ' that is currently registered with the shell's MenuCommandService
            ' 
            Private Shared ReadOnly s_packageCommandForwarderLists As New Dictionary(Of CommandID, LinkedList(Of DesignerMenuCommand))

            Public Shared Sub RegisterMenuCommandForwarder(commandID As CommandID, forwarder As DesignerMenuCommand)
                Dim menuCommandService As IMenuCommandService = VBPackage.Instance.MenuCommandService
                If menuCommandService IsNot Nothing Then
                    ' Remove previous active command (if any) and tell the shell that this is no longer the active 
                    ' command...
                    Dim previousCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If previousCommand IsNot Nothing Then
                        menuCommandService.RemoveCommand(previousCommand)
                    End If

                    ' Add the command to our internal list of commands...
                    AddMenuCommandForwarderToInternalList(commandID, forwarder)

                    menuCommandService.AddCommand(forwarder)
                Else
                    Debug.Fail("No package menu command service?")
                End If
            End Sub

            Public Shared Sub UnregisterMenuCommandForwarder(commandID As CommandID, forwarder As DesignerMenuCommand)
                Dim menuCommandService As IMenuCommandService = VBPackage.Instance.MenuCommandService
                If menuCommandService IsNot Nothing Then
                    ' Remove the currently active command (if any) from the MenuCommandService
                    Dim previousCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If previousCommand IsNot Nothing Then
                        menuCommandService.RemoveCommand(previousCommand)
                    End If

                    ' Update our internal list of commands
                    RemoveMenuCommandForwarderFromInternalList(commandID, forwarder)

                    ' Re-register the new command that is supposed to be active
                    Dim newCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If newCommand IsNot Nothing Then
                        menuCommandService.AddCommand(newCommand)
                    Else
                        ' Add an imposter command to keep an handler around when the UI is closed
                        Dim imposterCommand As ImposterDesignerMenuCommand = New ImposterDesignerMenuCommand(commandID)
                        AddMenuCommandForwarderToInternalList(commandID, imposterCommand)
                        menuCommandService.AddCommand(imposterCommand)
                    End If
                Else
                    Debug.Fail("No package menu command service?")
                End If
            End Sub

            ''' <summary>
            ''' Get the command at the head of the queue for the given command ID
            ''' </summary>
            ''' <returns>
            ''' The first command at the head of the queue, or NULL if no the queue is empty
            ''' </returns>
            Protected Shared Function GetMenuCommandAtHeadOfInternalList(cmdId As CommandID) As DesignerMenuCommand
                Dim list As LinkedList(Of DesignerMenuCommand) = Nothing
                If (Not s_packageCommandForwarderLists.TryGetValue(cmdId, list)) OrElse list Is Nothing OrElse list.Count = 0 Then
                    Return Nothing
                Else
                    Return list.First.Value
                End If
            End Function

            ''' <summary>
            ''' Add a menu command forwarder to our internal LIFO queue. 
            ''' If the command is in the list, but isn't the first command, we move it to the head of the list
            ''' </summary>
            Protected Shared Sub AddMenuCommandForwarderToInternalList(cmdId As CommandID, command As DesignerMenuCommand)
                Dim list As LinkedList(Of DesignerMenuCommand) = Nothing

                ' Demand create the list corresponding to this cmdId
                If Not s_packageCommandForwarderLists.TryGetValue(cmdId, list) Then
                    list = New LinkedList(Of DesignerMenuCommand)
                    s_packageCommandForwarderLists(cmdId) = list
                End If

                ' Move the command to the head of the queue...
                list.Remove(command)
                list.AddFirst(command)

                Debug.Assert(list.Count > 0, "We just added a menu command to the list - how come it is empty!")
            End Sub

            ''' <summary>
            ''' Remove a menu command forwarder from our internal LIFO queue. 
            ''' </summary>
            Protected Shared Sub RemoveMenuCommandForwarderFromInternalList(cmdId As CommandID, command As DesignerMenuCommand)
                Dim list As LinkedList(Of DesignerMenuCommand) = Nothing
                If s_packageCommandForwarderLists.TryGetValue(cmdId, list) Then
                    list.Remove(command)
                End If

                If list IsNot Nothing AndAlso list.Count = 0 Then
                    s_packageCommandForwarderLists.Remove(cmdId)
                End If
            End Sub

        End Class

#End Region

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="rootDesigner"></param>
        ''' <param name="serviceProvider"></param>
        ''' <param name="projectItem"></param>
        ''' <param name="namespaceToOverrideIfCustomToolIsEmpty">
        ''' If this is not Nothing, then setting a new custom tool value will also change the
        '''   custom tool namespace to this value, if the current custom tool is not empty.
        ''' 
        ''' This is currently used for the VB scenario - if the custom tool has been yet been set, and
        '''   the user turns on code generation, we want to also set the custom tool namespace to the
        '''   default for VB (My.Resources).
        ''' </param>
        Public Sub New(rootDesigner As BaseRootDesigner, serviceProvider As IServiceProvider, projectItem As EnvDTE.ProjectItem, namespaceToOverrideIfCustomToolIsEmpty As String)
            Requires.NotNull(rootDesigner, NameOf(rootDesigner))
            Requires.NotNull(projectItem, NameOf(projectItem))
            Requires.NotNull(serviceProvider, NameOf(serviceProvider))

            _rootDesigner = rootDesigner
            _projectItem = projectItem
            _serviceProvider = serviceProvider
            _namespaceToOverrideIfCustomToolIsEmpty = namespaceToOverrideIfCustomToolIsEmpty
        End Sub

        ''' <summary>
        ''' Adds the given code generator entry, using a language-dependent version of the accessibility as the display name
        ''' </summary>
        ''' <param name="accessibility"></param>
        ''' <param name="customToolValue"></param>
        Public Sub AddCodeGeneratorEntry(accessibility As AccessModifierConverter.Access, customToolValue As String)
            Debug.Assert([Enum].IsDefined(GetType(AccessModifierConverter.Access), accessibility))

            Dim entry As New CodeGeneratorWithDelayedName(accessibility, _serviceProvider, customToolValue)
            _codeGeneratorEntries.Add(entry)
            AddRecognizedCustomToolValue(entry.CustomToolValue)
        End Sub

        ''' <summary>
        ''' Add a mapping entry for a custom tool generator that we will show in the dropdown of available
        '''   choices
        ''' </summary>
        ''' <param name="displayName"></param>
        ''' <param name="customToolValue"></param>
        Public Sub AddCodeGeneratorEntry(displayName As String, customToolValue As String)
            Dim entry As New CodeGeneratorWithName(displayName, customToolValue)
            _codeGeneratorEntries.Add(entry)
            AddRecognizedCustomToolValue(entry.CustomToolValue)
        End Sub

        ''' <summary>
        ''' Add an entry for a custom tool generator that we recognize.  Adding it here does *not* mean
        '''   it will show up in the dropdown of available values.  Rather, it simply means that if this
        '''   value is found in the custom tool value, we won't disable the accessibility combobox.
        ''' It is okay to make multiple calls with the same value.  In fact, any generator added through
        '''   AddCodeGeneratorEntry will automatically be added here, too.
        ''' </summary>
        ''' <param name="customToolValue"></param>
        Public Sub AddRecognizedCustomToolValue(customToolValue As String)
            If Not _recognizedCustomToolValues.Contains(customToolValue) Then
                _recognizedCustomToolValues.Add(customToolValue)
                ' We also make sure to reset the cached value for if the custom tool(s)
                ' is/are registered...
                _customToolsRegistered = New Boolean?
            End If
        End Sub

        Protected ReadOnly Property RootDesigner As BaseRootDesigner
            Get
                Return _rootDesigner
            End Get
        End Property

        Protected Function GetMenuCommandsToRegister(commandIdCombobox As CommandID, commandIdGetDropdownValues As CommandID) As ICollection
            ' For a dynamic combobox, we need to add two commands, one to handle the combobox, and one to fill
            ' it with items...
            Dim MenuCommands As New List(Of MenuCommand)
            _designerCommandBarComboBoxCommand = New DesignerCommandBarComboBox(_rootDesigner, commandIdCombobox, AddressOf GetCurrentValue, AddressOf SetCurrentValue, AddressOf EnabledHandler)
            _commandIdCombobox = commandIdCombobox
            MenuCommands.Add(_designerCommandBarComboBoxCommand)
            MenuCommands.Add(New DesignerCommandBarComboBoxFiller(_rootDesigner, commandIdGetDropdownValues, AddressOf GetDropdownValues))

            RegisterMenuCommandForwarder()

            Return MenuCommands
        End Function

        ''' <summary>
        ''' Tries to retrieve the value of the "Custom Tool" property.  If there is no such
        '''   property in this project, returns False.
        ''' </summary>
        ''' <param name="value"></param>
        Private Function TryGetCustomToolPropertyValue(ByRef value As String) As Boolean
            value = Nothing

            Dim ToolProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(_projectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOL)
            If ToolProperty IsNot Nothing Then
                Dim CurrentCustomToolValue As String = TryCast(ToolProperty.Value, String)
                value = CurrentCustomToolValue
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Return the current accessibility value
        ''' </summary>
        Private Function GetCurrentValue() As String
            Dim currentValue As String
            Dim matchingEntry As CodeGenerator = GetCurrentMatchingGenerator()
            If matchingEntry IsNot Nothing Then
                currentValue = matchingEntry.DisplayName
            Else
                currentValue = My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_AccessModifier_Custom
            End If

            Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "GetCurrentValue: " & [GetType].Name & ": " & currentValue)
            Return currentValue
        End Function

        ''' <summary>
        ''' Searches the current custom tool value for a matching generator entry.
        ''' </summary>
        Private Function GetCurrentMatchingGenerator() As CodeGenerator
            Dim customToolValue As String = Nothing
            If TryGetCustomToolPropertyValue(customToolValue) Then
                For Each entry As CodeGenerator In _codeGeneratorEntries
                    If entry.CustomToolValue.Equals(customToolValue, StringComparison.OrdinalIgnoreCase) Then
                        Return entry
                    End If
                Next
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Set the current accessibility value
        ''' </summary>
        ''' <param name="value"></param>
        Private Sub SetCurrentValue(value As String)
            Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "SetCurrentValue: " & [GetType].Name & ": " & value)

            For Each entry As CodeGenerator In _codeGeneratorEntries
                If entry.DisplayName.Equals(value, StringComparison.CurrentCultureIgnoreCase) Then
                    TrySetCustomToolValue(entry.CustomToolValue)
                    Return
                End If
            Next

            'Couldn't find the expected entry.  Do nothing.
        End Sub

        ''' <summary>
        ''' Try to set the Custom Tool property to the given value.  Show an error dialog if
        '''   there's an error.
        ''' </summary>
        ''' <param name="value"></param>
        Private Sub TrySetCustomToolValue(value As String)
            Try
                Dim ToolProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(_projectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOL)
                Dim ToolNamespaceProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(_projectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOLNAMESPACE)

                If ToolProperty IsNot Nothing Then
                    Dim previousToolValue As String = TryCast(ToolProperty.Value, String)
                    If ToolNamespaceProperty IsNot Nothing Then
                        Dim previousToolNamespaceValue As String = Nothing
                        previousToolNamespaceValue = TryCast(ToolProperty.Value, String)
                    End If

                    ToolProperty.Value = value

                    If ToolNamespaceProperty IsNot Nothing _
                    AndAlso _namespaceToOverrideIfCustomToolIsEmpty IsNot Nothing _
                    AndAlso previousToolValue = "" Then
                        ' This is currently used for the VB scenario - if the custom tool has been yet been set, and
                        '   the user turns on code generation, we want to also set the custom tool namespace to the
                        '   default for VB (My.Resources).
                        ToolNamespaceProperty.Value = _namespaceToOverrideIfCustomToolIsEmpty
                    End If

                    _rootDesigner.RefreshMenuStatus()
                Else
                    Debug.Fail("Couldn't find CustomTool property.  Dropdown shouldn't have been enabled.")
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(TrySetCustomToolValue), NameOf(AccessModifierCombobox))
                DesignerMessageBox.Show(
                    _rootDesigner,
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Task_CantChangeCustomToolOrNamespace,
                    ex,
                    Nothing) 'Note: when we integrate the changes to DesignerMessageBox.Show, the caption property can be removed)
            End Try
        End Sub

        ''' <summary>
        ''' Gets the set of entries for the AccessModifier dropdown on the toolbar
        ''' </summary>
        Friend Function GetDropdownValues() As String()
            Dim Values As New List(Of String)

            For Each entry As CodeGenerator In _codeGeneratorEntries
                Values.Add(entry.DisplayName)
            Next

            Return Values.ToArray()
        End Function

        Protected MustOverride Function IsDesignerEditable() As Boolean

        Private Function EnabledHandler(MenuCommand As DesignerMenuCommand) As Boolean
            Try
                Dim shouldBeEnabled As Boolean = Me.ShouldBeEnabled()
                Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "EnabledHandler: " & [GetType].Name & ": Enabled=" & shouldBeEnabled)
            Catch ex As Exception When ReportWithoutCrash(ex, "Failed to determine if the access modifier combobox should be enabled", NameOf(AccessModifierCombobox))
                Throw
            End Try
            Return ShouldBeEnabled()
        End Function

        ''' <summary>
        ''' Is the AccessModifier combobox on the settings designer toolbar enabled?
        ''' </summary>
        Protected Overridable Function ShouldBeEnabled() As Boolean
            If Not IsDesignerEditable() Then
                Return False
            End If

            ' If the custom tool(s) aren't registered, we don't enable the combobox...
            If Not CustomToolRegistered Then
                Return False
            End If

            Dim customToolValue As String = Nothing
            If Not TryGetCustomToolPropertyValue(customToolValue) Then
                'This project has no Custom Tool property, so don't enable the dropdown.
                Return False
            End If

            'If the current custom tool is set to a (non-empty) single file generator that we don't
            '  recognize, then disable the combobox.  Otherwise the user might accidentally change
            '  it and won't easily be able to get back the original value.  This is an advanced 
            '  scenario, and the advanced user can change this value directly in the property sheet
            '  if really needed.
            If customToolValue <> "" AndAlso Not _recognizedCustomToolValues.Contains(customToolValue) Then
                Return False
            End If

            'Otherwise, we can enable it.
            Return True
        End Function

        ''' <summary>
        ''' Demand check if the custom tools that we know about are registered for the current project system.
        ''' </summary>
        Protected Overridable ReadOnly Property CustomToolRegistered As Boolean
            Get
                If Not _customToolsRegistered.HasValue Then
                    ' If one or more of the custom tools in the drop-down are not registered for the current
                    '  project type, we disable the combobox...
                    For Each generator As CodeGenerator In _codeGeneratorEntries
                        If Not ShellUtil.IsCustomToolRegistered(Hierarchy, generator.CustomToolValue) Then
                            _customToolsRegistered = False
                            Return _customToolsRegistered.Value
                        End If
                    Next

                    _customToolsRegistered = True
                End If

                Return _customToolsRegistered.Value
            End Get
        End Property

        ''' <summary>
        ''' Get the hierarchy from the associated project item
        ''' </summary>
        Protected Overridable ReadOnly Property Hierarchy As IVsHierarchy
            Get
                Return ShellUtil.VsHierarchyFromDTEProject(_serviceProvider, _projectItem.ContainingProject)
            End Get
        End Property

#Region "IDisposable"

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _isDisposed Then
                If disposing Then
                    UnregisterMenuCommandForwarder()
                End If
            End If

            _isDisposed = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

#Region "Menu command forwarding to the package.  See comments in DesignerMenuCommandForwarder"

        Friend Sub OnDesignerWindowActivated(activated As Boolean)
            If activated Then
                RegisterMenuCommandForwarder()
                'Note: we don't unregister it until we are Disposed.  This allow us
                '  to keep supplying the current text value of the combobox until
                '  another like editor gets activated or until our editor is closed.
            End If
        End Sub

        Protected Overridable Sub RegisterMenuCommandForwarder()
            DesignerMenuCommandForwarder.RegisterMenuCommandForwarder(_commandIdCombobox, _designerCommandBarComboBoxCommand)
        End Sub

        Protected Overridable Sub UnregisterMenuCommandForwarder()
            DesignerMenuCommandForwarder.UnregisterMenuCommandForwarder(_commandIdCombobox, _designerCommandBarComboBoxCommand)
        End Sub

#End Region

    End Class

End Namespace
