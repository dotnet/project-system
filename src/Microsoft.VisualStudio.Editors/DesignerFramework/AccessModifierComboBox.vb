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
    '''     Access modifier types for resources
    ''' </summary>
    Friend Enum AccessModifierType
        [Public]
        [Internal]
    End Enum

    ''' <summary>
    ''' Gets the language-dependent terminology for Public/Internal
    ''' </summary>
    Friend Class AccessModifierConverter
        Private ReadOnly _converter As TypeConverter

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
        ''' Gets the language-dependent terminology for Public/Internal
        ''' </summary>
        ''' <param name="accessibility"></param>
        Public Function ConvertToString(accessibility As AccessModifierType) As String
            Select Case accessibility
                Case AccessModifierType.Internal
                    If _converter IsNot Nothing Then
                        Return _converter.ConvertToString(MemberAttributes.Assembly)
                    Else
                        Return "Internal"
                    End If
                Case AccessModifierType.Public
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
        Private ReadOnly _resxFileProjectItem As EnvDTE.ProjectItem
        Private ReadOnly _serviceProvider As IServiceProvider
        Private ReadOnly _namespaceToOverrideIfCustomToolIsEmpty As String
        Private ReadOnly _codeGeneratorEntries As New List(Of CodeGenerator)
        Private ReadOnly _recognizedCustomToolValues As New List(Of String)

        Private _designerCommandBarComboBoxCommand As DesignerCommandBarComboBox
        Private _commandIdCombobox As CommandID
        Private _previousCustomToolValue As String

        ' Cached flag to indicate if the custom tools associated with this combobox are
        ' registered for the current project type.
        ' The states are True (registered), False (not registered) or Missing (we haven't 
        ' checked the project system yet)
        ' This field should only be accessed through the CustomToolsRegistered property.
        Private _customToolsRegistered As Boolean?

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

            Private ReadOnly _accessibility As AccessModifierType
            Private ReadOnly _serviceProvider As IServiceProvider

            Public Sub New(accessibility As AccessModifierType, serviceProvider As IServiceProvider, customToolValue As String)
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
                    Dim previousDesignerMenuCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If previousDesignerMenuCommand IsNot Nothing Then
                        menuCommandService.RemoveCommand(previousDesignerMenuCommand)
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
                    Dim previousDesignerMenuCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If previousDesignerMenuCommand IsNot Nothing Then
                        menuCommandService.RemoveCommand(previousDesignerMenuCommand)
                    End If

                    ' Update our internal list of commands
                    RemoveMenuCommandForwarderFromInternalList(commandID, forwarder)

                    ' Re-register the new command that is supposed to be active
                    Dim newDesignerMenuCommand As DesignerMenuCommand = GetMenuCommandAtHeadOfInternalList(commandID)
                    If newDesignerMenuCommand IsNot Nothing Then
                        menuCommandService.AddCommand(newDesignerMenuCommand)
                    Else
                        ' Add a dummy command to keep an handler around when the UI is closed
                        Dim dummyDesignerMenuCommand As New DummyDesignerMenuCommand(commandID)
                        AddMenuCommandForwarderToInternalList(commandID, dummyDesignerMenuCommand)
                        menuCommandService.AddCommand(dummyDesignerMenuCommand)
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
            _resxFileProjectItem = projectItem
            _serviceProvider = serviceProvider
            _namespaceToOverrideIfCustomToolIsEmpty = namespaceToOverrideIfCustomToolIsEmpty
        End Sub

        ''' <summary>
        ''' Adds the given code generator entry, using a language-dependent version of the accessibility as the display name
        ''' </summary>
        ''' <param name="accessibility"></param>
        ''' <param name="customToolValue"></param>
        Public Sub AddCodeGeneratorEntry(accessibility As AccessModifierType, customToolValue As String)
            Debug.Assert([Enum].IsDefined(GetType(AccessModifierType), accessibility))

            Dim codeGeneratorEntry As New CodeGeneratorWithDelayedName(accessibility, _serviceProvider, customToolValue)
            _codeGeneratorEntries.Add(codeGeneratorEntry)
            AddRecognizedCustomToolValue(codeGeneratorEntry.CustomToolValue)
        End Sub

        ''' <summary>
        ''' Add a mapping entry for a custom tool generator that we will show in the dropdown of available
        '''   choices
        ''' </summary>
        ''' <param name="displayName"></param>
        ''' <param name="customToolValue"></param>
        Public Sub AddCodeGeneratorEntry(displayName As String, customToolValue As String)
            Dim codeGeneratorEntry As New CodeGeneratorWithName(displayName, customToolValue)
            _codeGeneratorEntries.Add(codeGeneratorEntry)
            AddRecognizedCustomToolValue(codeGeneratorEntry.CustomToolValue)
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
            Dim menuCommands As New List(Of MenuCommand)
            _designerCommandBarComboBoxCommand = New DesignerCommandBarComboBox(_rootDesigner, commandIdCombobox, AddressOf GetCurrentAccessibilityValue, AddressOf SetCurrentAccessibilityValue, AddressOf EnabledHandler)
            _commandIdCombobox = commandIdCombobox
            menuCommands.Add(_designerCommandBarComboBoxCommand)
            menuCommands.Add(New DesignerCommandBarComboBoxFiller(_rootDesigner, commandIdGetDropdownValues, AddressOf GetDropdownValues))

            RegisterMenuCommandForwarder()

            Return menuCommands
        End Function

        ''' <summary>
        ''' Return the current accessibility value
        ''' </summary>
        Private Function GetCurrentAccessibilityValue() As String
            Dim currentValue As String
            Dim matchingCodeGenerator As CodeGenerator = GetCurrentMatchingGenerator()
            If matchingCodeGenerator IsNot Nothing Then
                currentValue = matchingCodeGenerator.DisplayName
            Else
                currentValue = My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_AccessModifier_Custom
            End If

            Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "GetCurrentAccessibilityValue: " & [GetType].Name & ": " & currentValue)
            Return currentValue
        End Function

        ''' <summary>
        ''' Searches the current custom tool value for a matching generator entry.
        ''' </summary>
        Private Function GetCurrentMatchingGenerator() As CodeGenerator
            Dim customToolValue As String = Nothing
            If TryGetCustomToolPropertyValue(customToolValue) Then
                For Each codeGenerator As CodeGenerator In _codeGeneratorEntries
                    If codeGenerator.CustomToolValue.Equals(customToolValue, StringComparison.OrdinalIgnoreCase) Then
                        Return codeGenerator
                    End If
                Next
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Tries to retrieve the value of the "Custom Tool" property.  If there is no such
        '''   property in this project, returns False.
        ''' </summary>
        ''' <param name="value"></param>
        Private Function TryGetCustomToolPropertyValue(ByRef value As String) As Boolean
            value = Nothing
            Dim customToolProperty As EnvDTE.Property = Nothing

            Try
                customToolProperty = DTEUtils.GetProjectItemProperty(_resxFileProjectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOL)
            Catch ex As KeyNotFoundException
                ' Possible limitation of Cps. In some cases Cps is not able to maintain the same item id for items,
                ' causing them to Not be found. In some scenarios (i.e., when the item Is moved), it ends up having
                ' a different id, so the older one can't be found anymore.
                If _previousCustomToolValue IsNot Nothing Then
                    value = _previousCustomToolValue
                    Return True
                End If
            End Try

            If customToolProperty IsNot Nothing Then
                Dim customToolValue As String = TryCast(customToolProperty.Value, String)
                value = customToolValue
                _previousCustomToolValue = customToolValue
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Set the current accessibility value
        ''' </summary>
        ''' <param name="value"></param>
        Private Sub SetCurrentAccessibilityValue(value As String)
            Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "SetCurrentAccessibilityValue: " & [GetType].Name & ": " & value)

            For Each codeGenerator As CodeGenerator In _codeGeneratorEntries
                If codeGenerator.DisplayName.Equals(value, StringComparison.CurrentCultureIgnoreCase) Then
                    TrySetCustomToolValue(codeGenerator.CustomToolValue)
                    Return
                End If
            Next

            'Couldn't find the expected codeGenerator.  Do nothing.
        End Sub

        ''' <summary>
        ''' Try to set the Custom Tool property to the given value.  Show an error dialog if
        '''   there's an error.
        ''' </summary>
        ''' <param name="value"></param>
        Private Sub TrySetCustomToolValue(value As String)
            Try
                Dim customToolProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(_resxFileProjectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOL)
                Dim customToolNamespaceProperty As EnvDTE.Property = DTEUtils.GetProjectItemProperty(_resxFileProjectItem, DTEUtils.PROJECTPROPERTY_CUSTOMTOOLNAMESPACE)

                If customToolProperty IsNot Nothing Then
                    Dim previousCustomToolValue As String = TryCast(customToolProperty.Value, String)

                    customToolProperty.Value = value

                    If customToolNamespaceProperty IsNot Nothing _
                    AndAlso _namespaceToOverrideIfCustomToolIsEmpty IsNot Nothing _
                    AndAlso previousCustomToolValue = "" Then
                        ' This is currently used for the VB scenario - if the custom tool has been yet been set, and
                        '   the user turns on code generation, we want to also set the custom tool namespace to the
                        '   default for VB (My.Resources).
                        customToolNamespaceProperty.Value = _namespaceToOverrideIfCustomToolIsEmpty
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
            Dim values As New List(Of String)

            For Each codeGenerator As CodeGenerator In _codeGeneratorEntries
                values.Add(codeGenerator.DisplayName)
            Next

            Return values.ToArray()
        End Function

        Protected MustOverride Function IsDesignerEditable() As Boolean

        Private Function EnabledHandler(MenuCommand As DesignerMenuCommand) As Boolean
            Try
                Dim shouldAccessModifierComboBoxBeEnabled As Boolean = ShouldBeEnabled()
                Switches.TracePDAccessModifierCombobox(TraceLevel.Verbose, "EnabledHandler: " & [GetType].Name & ": Enabled=" & shouldAccessModifierComboBoxBeEnabled)
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
                    For Each codeGenerator As CodeGenerator In _codeGeneratorEntries
                        If Not ShellUtil.IsCustomToolRegistered(Hierarchy, codeGenerator.CustomToolValue) Then
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
                Return ShellUtil.VsHierarchyFromDTEProject(_serviceProvider, _resxFileProjectItem.ContainingProject)
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
