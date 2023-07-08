' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Designer loader for settings files
    ''' </summary>
    Friend NotInheritable Class SettingsDesignerLoader
        Inherits DesignerFramework.BaseDesignerLoader
        Implements INameCreationService, IVsDebuggerEvents

#Region "Private fields"
        ''' References to the app.config file (if any)
        Private _appConfigDocData As DocData

        Private _serviceProvider As ServiceProvider

        Private _flushing As Boolean

        ' Set flag if we make changes to the settings object during load that should
        ' set the docdata to dirty immediately after we have loaded.
        Private _modifiedDuringLoad As Boolean

        ' Cached IVsDebugger from shell in case we don't have a service provider at
        ' shutdown so we can undo our event handler
        Private _vsDebugger As IVsDebugger
        Private _vsDebuggerEventsCookie As UInteger

        ' Current Debug mode
        Private _currentDebugMode As DBGMODE = DBGMODE.DBGMODE_Design

        ' BUGFIX: Dev11#45255 
        ' Hook up to build events so we can enable/disable the property 
        ' page while building
        Private WithEvents _buildEvents As EnvDTE.BuildEvents
        Private _readOnly As Boolean
#End Region

#Region "Base class overrides"

        ''' <summary>
        ''' Initialize the designer loader. This is called just after begin load, so we should
        ''' have a loader host here.
        ''' This is the place where we add services!
        ''' NOTE: Remember to call RemoveService on any service object we don't own, when the Loader is disposed
        '''  Otherwise, the service container will dispose those objects.
        ''' </summary>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            Dim projectItem As EnvDTE.ProjectItem = DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)

            ' Add my services
            LoaderHost.AddService(GetType(INameCreationService), Me)
            LoaderHost.AddService(GetType(ComponentSerializationService), New DesignerFramework.GenericComponentSerializationService(Nothing))

            ' Add our dynamic type service...
            Dim dynamicTypeService As DynamicTypeService =
                DirectCast(_serviceProvider.GetService(GetType(DynamicTypeService)), DynamicTypeService)

            Dim cm As EnvDTE.CodeModel = Nothing
            If projectItem IsNot Nothing AndAlso projectItem.ContainingProject IsNot Nothing AndAlso projectItem.ContainingProject.CodeModel IsNot Nothing Then
                cm = projectItem.ContainingProject.CodeModel
            End If

            ' Try to add our typename resolution component
            If cm IsNot Nothing Then
                LoaderHost.AddService(GetType(SettingTypeNameResolutionService), New SettingTypeNameResolutionService(cm.Language))
            Else
                LoaderHost.AddService(GetType(SettingTypeNameResolutionService), New SettingTypeNameResolutionService(""))
            End If

            ' Add settings type cache...
            If cm IsNot Nothing Then
                LoaderHost.AddService(GetType(SettingsTypeCache), New SettingsTypeCache(VsHierarchy, ProjectItemid, dynamicTypeService.GetTypeResolutionService(VsHierarchy, ProjectItemid), cm.IsCaseSensitive))
            Else
                LoaderHost.AddService(GetType(SettingsTypeCache), New SettingsTypeCache(VsHierarchy, ProjectItemid, dynamicTypeService.GetTypeResolutionService(VsHierarchy, ProjectItemid), True))
            End If
            LoaderHost.AddService(GetType(SettingsValueCache), New SettingsValueCache(Globalization.CultureInfo.InvariantCulture))

            ' Listen for change notifications
            Dim ComponentChangeService As IComponentChangeService = CType(GetService(GetType(IComponentChangeService)), IComponentChangeService)
            AddHandler ComponentChangeService.ComponentAdded, AddressOf ComponentAddedHandler
            AddHandler ComponentChangeService.ComponentChanging, AddressOf ComponentChangingHandler
            AddHandler ComponentChangeService.ComponentChanged, AddressOf ComponentChangedHandler
            AddHandler ComponentChangeService.ComponentRemoved, AddressOf ComponentRemovedHandler

        End Sub

        ''' <summary>
        ''' Initialize this instance
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <param name="Hierarchy">Hierarchy (project) for item to load</param>
        ''' <param name="ItemId">ItemId in Hierarchy to load</param>
        ''' <param name="punkDocData">Document data to load</param>
        Friend Overrides Sub InitializeEx(ServiceProvider As ServiceProvider, moniker As String, Hierarchy As IVsHierarchy, ItemId As UInteger, punkDocData As Object)
            MyBase.InitializeEx(ServiceProvider, moniker, Hierarchy, ItemId, punkDocData)

            _serviceProvider = ServiceProvider

            SetSingleFileGenerator()
        End Sub

        ''' <summary>
        ''' Overrides base Dispose.
        ''' </summary>
        Public Overrides Sub Dispose()
            'Remove services we proffered.
            '
            'Note: LoaderHost.RemoveService does not raise any exceptions if the service we're trying to
            '  remove isn't already there, so there's no need for a try/catch.
            LoaderHost.RemoveService(GetType(INameCreationService))
            LoaderHost.RemoveService(GetType(ComponentSerializationService))

            LoaderHost.RemoveService(GetType(SettingTypeNameResolutionService))
            LoaderHost.RemoveService(GetType(SettingsTypeCache))
            LoaderHost.RemoveService(GetType(SettingsValueCache))
            MyBase.Dispose()
        End Sub

        ''' <summary>
        ''' Name of base component this loader (de)serializes
        ''' </summary>
        ''' <returns>Name of base component this loader (de)serializes</returns>
        Protected Overrides Function GetBaseComponentClassName() As String
            Return GetType(DesignTimeSettings).Name
        End Function

        ''' <summary>
        ''' Flush any changes to underlying docdata(s)
        ''' </summary>
        ''' <param name="SerializationManager"></param>
        Protected Overrides Sub HandleFlush(SerializationManager As IDesignerSerializationManager)
            Try
                _flushing = True
                Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
                If Designer IsNot Nothing Then
                    Designer.CommitPendingChanges(True, False)
                Else
                    Debug.Fail("Failed to get designer from my root component!")
                End If

                Dim SettingsWriter As DocDataTextWriter = Nothing
                Try

                    SettingsWriter = New DocDataTextWriter(m_DocData)

                    SettingsSerializer.Serialize(RootComponent, GeneratedClassNamespace(), GeneratedClassName, SettingsWriter, DesignerFramework.DesignUtil.GetEncoding(DocData))
                Finally
                    If SettingsWriter IsNot Nothing Then
                        SettingsWriter.Close()
                    End If
                End Try

                ' Flush values to app.config file
                FlushAppConfig()
            Finally
                _flushing = False
            End Try
        End Sub

        ''' <summary>
        ''' Load the settings designer. Create a new DesignTimeSettings instance, add it to my loader host, deserialize
        ''' the contents of "my" docdata.
        ''' </summary>
        ''' <param name="SerializationManager">My serialization manager</param>
        Protected Overrides Sub HandleLoad(SerializationManager As IDesignerSerializationManager)
            Switches.TraceSDSerializeSettings(TraceLevel.Info, "SettingsDesignerLoader: Start loading settings")
            Debug.Assert(LoaderHost IsNot Nothing, "Asked to load settings designer without a LoaderHost!?")
            LoaderHost.CreateComponent(GetType(DesignTimeSettings))
            Debug.Assert(RootComponent IsNot Nothing, "Failed to create DesignTimeSettings root component - failure should throw exception!?")

            ' Let's check the size of the buffer to read...
            Dim BufSize As Integer
            VSErrorHandler.ThrowOnFailure(DocData.Buffer.GetSize(BufSize))

            '...and if it is NOT empty, we should try to deserialize it
            If BufSize > 0 Then
                ' We have data - let's deserialize!
                Dim SettingsReader As DocDataTextReader = Nothing
                Try
                    SettingsReader = New DocDataTextReader(m_DocData)
                    SettingsSerializer.Deserialize(RootComponent, SettingsReader, False)
                Catch ex As Exception
                    ReportSerializationError(SerializationManager, ex)
                    Throw New InvalidOperationException(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_Err_CantLoadSettingsFile, ex)
                Finally
                    If SettingsReader IsNot Nothing Then
                        SettingsReader.Close()
                    End If
                End Try

                Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "SettingsDesignerLoader: Done loading settings reader")
            Else
                ' The buffer was empty - no panic, this is probably just a new file
            End If

            AttachAppConfigDocData(False)

            If _appConfigDocData IsNot Nothing Then
                Switches.TraceSDSerializeSettings(TraceLevel.Verbose, "Loading app.config")
                LoadAppConfig()
            End If
        End Sub

        Private Shared Sub ReportSerializationError(SerializationManager As IDesignerSerializationManager, ex As Exception)
            If SerializationManager IsNot Nothing Then
                Dim userErrorMessage As String =
                    ex.Message +
                    Environment.NewLine +
                    Environment.NewLine +
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_Err_CantLoadSettingsFile +
                    Environment.NewLine +
                    Environment.NewLine +
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_HelpMessage_SuggestFileOpenWith

                Dim exWithHelp = New Exception(userErrorMessage) With {
                    .HelpLink = HelpIDs.Err_LoadingSettingsFile
                }
                SerializationManager.ReportError(exWithHelp)
            End If
        End Sub

#End Region

#Region "Private helper properties"
        ''' <summary>
        ''' Get access to "our" root component
        ''' </summary>
        Private ReadOnly Property RootComponent As DesignTimeSettings
            Get
                Try
                    If LoaderHost Is Nothing Then
                        Debug.Fail("No loader host?")
                        Return Nothing
                    Else
                        Return CType(LoaderHost.RootComponent, DesignTimeSettings)
                    End If
                Catch Ex As ObjectDisposedException
                    Debug.Fail("Our loader host is disposed!")
                    Throw
                End Try
            End Get
        End Property

        Friend ReadOnly Property GeneratedClassName As String
            Get
                Return SettingsDesigner.GeneratedClassName(VsHierarchy, ProjectItemid, RootComponent, DocData.Name)
            End Get
        End Property

        Private ReadOnly Property GeneratedClassNamespace As String
            Get
                Return ProjectUtils.GeneratedSettingsClassNamespace(VsHierarchy, ProjectItemid)
            End Get
        End Property

        Private ReadOnly Property GeneratedClassNamespace(IncludeRootNamespace As Boolean) As String
            Get
                Return ProjectUtils.GeneratedSettingsClassNamespace(VsHierarchy, ProjectItemid, IncludeRootNamespace)
            End Get
        End Property

#End Region

#Region "Private helper functions"

        ''' <summary>
        ''' Get a DocData for the App.Config file (if any)
        ''' </summary>
        Private Function AttachAppConfigDocData(CreateIfNotExist As Boolean) As Boolean
            ' Now, Let's try and get to the app.config file
            If _appConfigDocData Is Nothing Then
                _appConfigDocData = AppConfigSerializer.GetAppConfigDocData(VBPackage.Instance, VsHierarchy, CreateIfNotExist, False, DocDataService)
                If _appConfigDocData IsNot Nothing Then
                    AddHandler _appConfigDocData.DataChanged, AddressOf ExternalChange
                End If
            End If
            Return _appConfigDocData IsNot Nothing
        End Function

        ''' <summary>
        ''' Make sure that we have a custom tool associated with this file
        ''' </summary>
        Public Sub SetSingleFileGenerator()
            Dim ProjectItem As EnvDTE.ProjectItem = DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)
            If ProjectItem IsNot Nothing AndAlso ProjectItem.Properties IsNot Nothing Then
                Debug.Assert(ProjectItemid = ProjectUtils.ItemId(VsHierarchy, ProjectItem))
                Try
                    Dim CustomToolProperty As EnvDTE.Property = ProjectItem.Properties.Item("CustomTool")
                    If CustomToolProperty IsNot Nothing Then
                        Dim CurrentCustomTool As String = TryCast(CustomToolProperty.Value, String)
                        If CurrentCustomTool = "" Then
                            CustomToolProperty.Value = SettingsSingleFileGenerator.SingleFileGeneratorName
                        End If
                    End If
                Catch ex As ArgumentException
                    ' Venus doesn't like people looking for the "CustomTool" property of the project item...
                    ' Well, if that's the case we can't very well the the property either.... no big deal!
                End Try
            End If
        End Sub

#End Region

#Region "Component change notifications"
        ''' <summary>
        ''' Whenever someone added components to the host, we've gotta make sure that the component is
        ''' added to the settings
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentAddedHandler(sender As Object, e As ComponentEventArgs)
            Dim designTimeSettingInstance = TryCast(e.Component, DesignTimeSettingInstance)
            If designTimeSettingInstance IsNot Nothing Then
                ' Let's make sure our root component knows about this setting instance!
                Debug.Assert(RootComponent IsNot Nothing, "No root component when adding design time setting instances")
                RootComponent.Add(designTimeSettingInstance)
            End If
        End Sub

        ''' <summary>
        ''' Indicate that a component is about to be changed
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentChangingHandler(sender As Object, e As ComponentChangingEventArgs)
            Dim instance As DesignTimeSettingInstance = TryCast(e.Component, DesignTimeSettingInstance)
            ' If this is a rename of a web reference, we have to check out the project file in order to update
            ' the corresponding property in it...
            If instance IsNot Nothing _
                AndAlso e.Member IsNot Nothing _
                AndAlso e.Member.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) _
                AndAlso String.Equals(instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) _
            Then
                If _serviceProvider IsNot Nothing _
                    AndAlso ProjectItem IsNot Nothing _
                    AndAlso ProjectItem.ContainingProject IsNot Nothing _
                    AndAlso ProjectItem.ContainingProject.FullName <> "" _
                Then
                    ' Check out the project file...
                    Dim filesToCheckOut As New List(Of String)(1) From {
                        ProjectItem.ContainingProject.FullName
                    }
                    DesignerFramework.SourceCodeControlManager.QueryEditableFiles(_serviceProvider, filesToCheckOut, True, False)
                End If
            End If
        End Sub

        ''' <summary>
        ''' When the name of a setting is changed, we have to rename the symbol...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentChangedHandler(sender As Object, e As ComponentChangedEventArgs)
            ' If the name property of a designtimesetting instance (or derived class) has changed in a project item
            ' that has "our" custom tool associated with it, we want to invoke a global rename of the setting
            '
            If TypeOf e.Component Is DesignTimeSettingInstance _
                AndAlso e.Member.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) _
                AndAlso Not String.Equals(TryCast(e.OldValue, String), TryCast(e.NewValue, String), StringComparison.Ordinal) _
            Then
                Dim SettingsFileProjectItem As EnvDTE.ProjectItem = DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)

                If SettingsFileProjectItem IsNot Nothing AndAlso SettingsFileProjectItem.Properties IsNot Nothing Then
                    Dim CurrentCustomTool As String
                    Try
                        Dim CustomToolProperty As EnvDTE.Property = SettingsFileProjectItem.Properties.Item("CustomTool")
                        CurrentCustomTool = TryCast(CustomToolProperty.Value, String)
                    Catch ex As ArgumentException
                        ' No problems, this is probably just a venus project!
                        Return
                    End Try

                    ' We only rename the symbol if the current custom tool is our file generator...
                    If CurrentCustomTool IsNot Nothing AndAlso
                        (
                            CurrentCustomTool.Equals(SettingsSingleFileGenerator.SingleFileGeneratorName, StringComparison.OrdinalIgnoreCase) _
                            OrElse CurrentCustomTool.Equals(PublicSettingsSingleFileGenerator.SingleFileGeneratorName, StringComparison.OrdinalIgnoreCase)
                        ) _
                    Then
                        Dim GeneratedClassName As String = SettingsDesigner.FullyQualifiedGeneratedTypedSettingsClassName(VsHierarchy, ProjectItemid, RootComponent, SettingsFileProjectItem)
                        Dim FindSettingClassFilter As New ProjectUtils.KnownClassName(GeneratedClassName)
                        Dim ce As EnvDTE.CodeElement = ProjectUtils.FindElement(SettingsFileProjectItem,
                                                                        False,
                                                                        True,
                                                                        FindSettingClassFilter)

                        If ce Is Nothing Then
                            ' If our custom tool haven't run yet, we won't find a typed wrapper class in the project...
                            ' Consider: should we force the custom generator to run here - it's probably too late since we already have changed the name...?
                            Return
                        End If

                        Dim FindSettingsPropertyFilter As New ProjectUtils.FindPropertyFilter(ce, DirectCast(e.OldValue, String))
                        Dim pce As EnvDTE.CodeElement = ProjectUtils.FindElement(SettingsFileProjectItem, True, True, FindSettingsPropertyFilter)

                        If pce Is Nothing Then
                            ' If we can't find the property in the strongly typed settings class, it may be because the file hasn't
                            ' been regenerated yet...
                            ' Consider: should we force the custom generator to run here - it's probably too late since we already have changed the name...?
                            Return
                        End If

                        Dim pce2 As EnvDTE80.CodeElement2 = TryCast(pce, EnvDTE80.CodeElement2)
                        If pce2 Is Nothing Then
                            Debug.Fail("Failed to get CodeElement2 interface from CodeElement - CodeModel doesn't support ReplaceSymbol?")
                        Else
                            Try
                                SettingsSingleFileGeneratorBase.AllowSymbolRename = True
                                pce2.RenameSymbol(DirectCast(e.NewValue, String))
                            Catch ex As COMException When ex.ErrorCode = CodeModelUtils.HR_E_CSHARP_USER_CANCEL _
                                                          OrElse ex.ErrorCode = NativeMethods.E_ABORT _
                                                          OrElse ex.ErrorCode = NativeMethods.OLECMDERR_E_CANCELED _
                                                          OrElse ex.ErrorCode = NativeMethods.E_FAIL
                                ' We should ignore if the customer cancels this or we can not build the project...
                            Catch ex As Exception When ReportWithoutCrash(ex, "Failed to rename symbol", NameOf(SettingsDesignerLoader))
                                DesignerFramework.DesignerMessageBox.Show(_serviceProvider, ex, DesignerFramework.DesignUtil.GetDefaultCaption(_serviceProvider))
                            Finally
                                SettingsSingleFileGeneratorBase.AllowSymbolRename = False
                            End Try
                        End If
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Whenever someone removed components from the host, we've gotta make sure that the component is
        ''' removed from the settings
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentRemovedHandler(sender As Object, e As ComponentEventArgs)
            If LoaderHost IsNot Nothing AndAlso LoaderHost.Loading Then
                ' If we are currently (re)loading the design surface, we don't want to force-run the custom tool
                ' (Loading is set to true during reload as well as during load)
                Return
            End If

            Dim designTimeSettingInstance = TryCast(e.Component, DesignTimeSettingInstance)
            If designTimeSettingInstance IsNot Nothing Then
                If RootComponent IsNot Nothing Then
                    ' Let's make sure our root component knows about this setting instance!
                    RootComponent.Remove(designTimeSettingInstance)

                    ' We need to make sure that we run the custom tool whenever we remove a setting - if we don't 
                    ' we may run into problems later if we try to rename the setting to a setting that we
                    ' have already removed...
                    RunSingleFileGenerator(True)
                End If
            End If
        End Sub
#End Region

#Region "Other change notifications"

        ''' <summary>
        ''' An external change was made to one of my docdatas. Reload designer!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ExternalChange(sender As Object, e As EventArgs)
            If Not _flushing Then
                Debug.Assert(_appConfigDocData IsNot Nothing, "Why did we get a change notification for a NULL App.Config DocData?")
                Switches.TraceSDSerializeSettings(TraceLevel.Info, "Queueing a reload due to an external change of the app.config DocData")
                Reload(ReloadOptions.NoFlush)
            End If
        End Sub

        ''' <summary>
        ''' Load the contents of the app.config file
        ''' </summary>
        Private Sub LoadAppConfig()
            Debug.Assert(_appConfigDocData IsNot Nothing, "Can't load a non-existing app.config file!")
            Try
                Dim cfgHelper As New ConfigurationHelperService

                Dim objectDirty As AppConfigSerializer.DirtyState =
                    AppConfigSerializer.Deserialize(RootComponent,
                                                    DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache),
                                                    cfgHelper.GetSectionName(ProjectUtils.FullyQualifiedClassName(GeneratedClassNamespace(True), GeneratedClassName), String.Empty),
                                                    _appConfigDocData,
                                                    AppConfigSerializer.MergeValueMode.Prompt,
                                                    CType(GetService(GetType(System.Windows.Forms.Design.IUIService)), System.Windows.Forms.Design.IUIService))
                If objectDirty <> AppConfigSerializer.DirtyState.NoChange Then
                    ' Set flag if we make changes to the settings object during load that should
                    ' set the docdata to dirty immediately after we have loaded.
                    ' 
                    ' Since component change notifications are ignored while we are loading the object,
                    ' we have to do this after the load is completed....
                    _modifiedDuringLoad = True
                End If
            Catch ex As Configuration.ConfigurationErrorsException
                ' We failed to load the app config xml document....
                DesignerFramework.DesignUtil.ReportError(_serviceProvider, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_FailedToLoadAppConfigValues, HelpIDs.Err_LoadingAppConfigFile)
            Catch Ex As Exception When ReportWithoutCrash(Ex, "Failed to load app.config", NameOf(SettingsDesignerLoader))
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' If we made changes during load that will affect the docdata, this is the place to set the modified flag...
        ''' </summary>
        ''' <param name="successful"></param>
        ''' <param name="errors"></param>
        Protected Overrides Sub OnEndLoad(successful As Boolean, errors As ICollection)
            MyBase.OnEndLoad(successful, errors)
            ConnectDebuggerEvents()
            ConnectBuildEvents()
            'test if in build process
            If IsInBuildProgress() Then
                SetReadOnlyMode(True, String.Empty)
                _readOnly = True
            Else
                SetReadOnlyMode(False, String.Empty)
                _readOnly = False
            End If
            If _modifiedDuringLoad AndAlso IsDesignerEditable() Then
                Try
                    OnModifying()
                    Modified = True
                Catch ex As CheckoutException
                    ' What should we do here???
                End Try
            End If
            _modifiedDuringLoad = False
        End Sub

        ''' <summary>
        ''' Persist our values to the app.config file...
        ''' </summary>
        Private Sub FlushAppConfig()
            If AttachAppConfigDocData(True) Then
                Debug.Assert(_appConfigDocData IsNot Nothing, "Why did AttachAppConfigDocData return true when we don't have an app.config docdata!?")
                Try
                    AppConfigSerializer.Serialize(RootComponent,
                                DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache),
                                DirectCast(GetService(GetType(SettingsValueCache)), SettingsValueCache),
                                GeneratedClassName,
                                GeneratedClassNamespace(True),
                                _appConfigDocData,
                                VsHierarchy,
                                True)
                Catch Ex As Exception When ReportWithoutCrash(Ex, "Failed to flush values to the app config document", NameOf(SettingsDesignerLoader))
                    ' We failed to flush values to the app config document....
                    DesignerFramework.DesignUtil.ReportError(_serviceProvider, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_FailedToSaveAppConfigValues, HelpIDs.Err_SavingAppConfigFile)
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Called when the document's window is activated or deactivated
        ''' </summary>
        ''' <param name="Activated"></param>
        Protected Overrides Sub OnDesignerWindowActivated(Activated As Boolean)
            MyBase.OnDesignerWindowActivated(Activated)

            Switches.TracePDFocus(TraceLevel.Warning, "SettingsDesignerLoader.OnDesignerWindowActivated")
            Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
            If Designer IsNot Nothing AndAlso Designer.HasView Then
                Dim view As SettingsDesignerView = DirectCast(Designer.GetView(ViewTechnology.Default), SettingsDesignerView)
                view.OnDesignerWindowActivated(Activated)
                If Not Activated Then
                    Switches.TracePDFocus(TraceLevel.Warning, "... SettingsDesignerLoader.OnDesignerWindowActivated: CommitPendingChanges()")
                    view.CommitPendingChanges(True, True)
                End If
            End If
        End Sub
#End Region

        ''' <summary>
        '''  Dispose any owned resources
        ''' </summary>
        ''' <param name="Disposing"></param>
        Protected Overrides Sub Dispose(Disposing As Boolean)
            If Disposing Then
                ' The LoaderHost will remove all services all by itself...
                ' No need to worry about that here :)

                Dim ComponentChangeService As IComponentChangeService = CType(GetService(GetType(IComponentChangeService)), IComponentChangeService)

                If ComponentChangeService IsNot Nothing Then
                    RemoveHandler ComponentChangeService.ComponentAdded, AddressOf ComponentAddedHandler
                    RemoveHandler ComponentChangeService.ComponentChanging, AddressOf ComponentChangingHandler
                    RemoveHandler ComponentChangeService.ComponentChanged, AddressOf ComponentChangedHandler
                    RemoveHandler ComponentChangeService.ComponentRemoved, AddressOf ComponentRemovedHandler
                End If

                ' Unregister any change handlers that we've associated with the app.config file
                If _appConfigDocData IsNot Nothing Then
                    RemoveHandler _appConfigDocData.DataChanged, AddressOf ExternalChange

                    ' 
                    ' DevDiv 79301:
                    ' If our primary docdata is not modified, but some other editor has made the app.config file docdata
                    ' dirty, and we happen to be the last editor open, then we need to dispose the app.config docdata in
                    ' order to save any changes.
                    '
                    ' The DesignerDocDataService that normally handles this will not save dependent files unless the primary
                    ' docdata is modified...
                    '
                    If m_DocData IsNot Nothing AndAlso (Not m_DocData.Modified) AndAlso _appConfigDocData.Modified Then
                        _appConfigDocData.Dispose()
                    Else
                        ' 
                        ' Please note that we should normally let the DesignerDocDataService's dispose dispose
                        ' the child docdata - this will be done in the BaseDesignerLoader's Dispose.
                        ' 
                    End If
                    _appConfigDocData = Nothing
                End If

                DisconnectDebuggerEvents()
            End If
            MyBase.Dispose(Disposing)
        End Sub

        Friend Function EnsureCheckedOut() As Boolean

            If Not IsDesignerEditable() Then
                Return False
            End If

            Try
                Dim ProjectReloaded As Boolean
                ManualCheckOut(ProjectReloaded)
                If ProjectReloaded Then
                    'If the project was reloaded, clients need to exit as soon as possible.
                    Return False
                End If

                AttachAppConfigDocData(True)

                Switches.TracePDFocus(TraceLevel.Warning, "[disabled] SettingsDesignerLoader EnsureCheckedOut hack: Me.LoaderHost.Activate()")

                Return True
            Catch ex As CheckoutException
                ' We failed to checkout the file (s)...
                Return False
            End Try
        End Function

        ''' <summary>
        ''' We sometimes want to check out the project file (to add app.config) and sometimes we only want to 
        ''' check out the app.config file itself...
        ''' </summary>
        Protected Overrides ReadOnly Property ManagingDynamicSetOfFiles As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Overridden in order to provide the app.config file name or the project name when as well as the project item
        ''' and dependent file...
        ''' </summary>
        Friend Overrides ReadOnly Property FilesToCheckOut As List(Of String)
            Get
                Dim result As List(Of String) = MyBase.FilesToCheckOut
                Dim projectItem As EnvDTE.ProjectItem = DTEUtils.ProjectItemFromItemId(VsHierarchy, ProjectItemid)
                Dim appConfigOrProjectName As String = ProjectUtils.AppConfigOrProjectFileNameForCheckout(projectItem, VsHierarchy)
                If appConfigOrProjectName <> "" Then
                    result.Add(appConfigOrProjectName)
                End If
                Return result
            End Get
        End Property

        Private Function InDesignMode() As Boolean
            Return _currentDebugMode = DBGMODE.DBGMODE_Design
        End Function

#Region "INameCreationService"
        Public Function CreateName(container As ComponentModel.IContainer, dataType As Type) As String Implements INameCreationService.CreateName
            If dataType IsNot Nothing AndAlso String.Equals(dataType.AssemblyQualifiedName, GetType(DesignTimeSettings).AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase) Then
                Return ""
            End If

            Dim Settings As DesignTimeSettings = RootComponent
            If Settings IsNot Nothing Then
                Return Settings.CreateUniqueName()
            End If

            Debug.Fail("You should never reach this line of code!")
            Dim existingNames As New Hashtable
            For i As Integer = 0 To container.Components.Count - 1
                Dim instance As DesignTimeSettingInstance = TryCast(container.Components.Item(i), DesignTimeSettingInstance)
                If instance IsNot Nothing Then
                    existingNames(instance.Name) = Nothing
                End If
            Next

            For i As Integer = 1 To container.Components.Count + 1
                Dim SuggestedName As String = "Setting" & i.ToString()
                If Not existingNames.ContainsKey(SuggestedName) Then
                    Return SuggestedName
                End If
            Next
            Debug.Fail("You should never reach this line of code!")
            Return ""
        End Function

        Public Function IsValidName(name As String) As Boolean Implements INameCreationService.IsValidName
            If RootComponent IsNot Nothing Then
                Return RootComponent.IsValidName(name)
            Else
                Return name <> ""
            End If
        End Function

        Public Sub ValidateName(name As String) Implements INameCreationService.ValidateName
            If Not IsValidName(name) Then
                Throw CreateArgumentException(NameOf(name))
            End If
        End Sub
#End Region

#Region "ReadOnly during debug mode and build" ' BUGFIX: Dev11#45255 

        ''' <summary>
        ''' Start listening to build events and set our initial build status
        ''' </summary>
        Private Sub ConnectBuildEvents()
            Dim dte As EnvDTE.DTE
            dte = CType(GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
            If dte IsNot Nothing Then
                _buildEvents = dte.Events.BuildEvents
            Else
                Debug.Fail("No DTE - can't hook up build events - we don't know if start/stop building...")
            End If
        End Sub

        ''' <summary>
        '''     Returns a value indicating whether the designer is currently editable; that is, 
        '''     we're not debugging in any form and the solution is not currently building.
        ''' </summary>
        Friend Function IsDesignerEditable() As Boolean

            Return InDesignMode() AndAlso Not _readOnly

        End Function

        ''' <summary>
        ''' A build has started - disable/enable page
        ''' </summary>
        Private Sub BuildBegin(scope As EnvDTE.vsBuildScope, action As EnvDTE.vsBuildAction) Handles _buildEvents.OnBuildBegin
            SetReadOnlyMode(True, String.Empty)
            _readOnly = True
            RefreshView()
        End Sub

        ''' <summary>
        ''' A build has finished - disable/enable page
        ''' </summary>
        ''' <param name="scope"></param>
        ''' <param name="action"></param>
        Private Sub BuildDone(scope As EnvDTE.vsBuildScope, action As EnvDTE.vsBuildAction) Handles _buildEvents.OnBuildDone
            SetReadOnlyMode(False, String.Empty)
            _readOnly = False
            RefreshView()
        End Sub

        ''' <summary>
        ''' Refresh the status of Settings view commands
        ''' </summary>
        Private Sub RefreshView()
            Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
            If Designer IsNot Nothing AndAlso Designer.HasView Then
                Dim view As SettingsDesignerView = DirectCast(Designer.GetView(ViewTechnology.Default), SettingsDesignerView)
                view.RefreshCommandStatus()
            End If
        End Sub

        ''' <summary>
        ''' Hook up with the debugger event mechanism to determine current debug mode
        ''' </summary>
        Private Sub ConnectDebuggerEvents()
            If _vsDebuggerEventsCookie = 0 Then
                _vsDebugger = CType(GetService(GetType(IVsDebugger)), IVsDebugger)
                If _vsDebugger IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(_vsDebugger.AdviseDebuggerEvents(Me, _vsDebuggerEventsCookie))

                    Dim mode As DBGMODE() = New DBGMODE() {DBGMODE.DBGMODE_Design}
                    'Get the current mode
                    VSErrorHandler.ThrowOnFailure(_vsDebugger.GetMode(mode))
                    OnModeChange(mode(0))
                Else
                    Debug.Fail("Cannot obtain IVsDebugger from shell")
                    OnModeChange(DBGMODE.DBGMODE_Design)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Unhook event notification for debugger 
        ''' </summary>
        Private Sub DisconnectDebuggerEvents()
            Try
                If _vsDebugger IsNot Nothing AndAlso _vsDebuggerEventsCookie <> 0 Then
                    VSErrorHandler.ThrowOnFailure(_vsDebugger.UnadviseDebuggerEvents(_vsDebuggerEventsCookie))
                    _vsDebuggerEventsCookie = 0
                    _vsDebugger = Nothing
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(DisconnectDebuggerEvents), NameOf(SettingsDesignerLoader))
            End Try
        End Sub

        ''' <summary>
        ''' handle DebugMode change event, disable the designer when in debug mode...
        ''' </summary>
        ''' <param name="dbgmodeNew"></param>
        Private Function OnModeChange(dbgmodeNew As DBGMODE) As Integer Implements IVsDebuggerEvents.OnModeChange
            Try
                If dbgmodeNew = DBGMODE.DBGMODE_Design Then
                    SetReadOnlyMode(False, String.Empty)
                ElseIf _currentDebugMode = DBGMODE.DBGMODE_Design Then
                    SetReadOnlyMode(True, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_CantEditInDebugMode)
                End If
            Finally
                _currentDebugMode = dbgmodeNew
            End Try

            ' Let's try to refresh whatever menus we have here...
            If LoaderHost IsNot Nothing AndAlso RootComponent IsNot Nothing Then
                Dim Designer As SettingsDesigner = DirectCast(LoaderHost.GetDesigner(RootComponent), SettingsDesigner)
                If Designer IsNot Nothing Then
                    Designer.RefreshMenuStatus()
                End If
            End If
        End Function
#End Region

    End Class

End Namespace
