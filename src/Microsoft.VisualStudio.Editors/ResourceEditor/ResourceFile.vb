' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.CodeDom.Compiler
Imports System.ComponentModel.Design
Imports System.IO
Imports System.Resources
Imports System.Windows.Forms
Imports System.Xml

Imports Microsoft.Internal.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner
Imports Microsoft.Win32

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A representation of a resx file (essentially a ResourceCollection).  Wraps the
    '''   reading and writing of the file, plus the management of the resources
    '''   (instances of the Resource class) within it.
    ''' </summary>
    Friend Class ResourceFile
        Implements IDisposable
        Implements ResourceTypeEditor.IResourceContentFile

#Region "Fields"

        'A pointer to the host's IComponentChangeService.  We use this to get notified
        '  when components (Resource instances) are added/removed from the collection
        '  (both when we do it manually and when Undo/Redo does it for us), etc.
        Private WithEvents _componentChangeService As IComponentChangeService

        'Our set of resources
        Private ReadOnly _resources As Dictionary(Of String, Resource)

        'Metadata from the resource file so we can write them back out when saving the file.
        Private ReadOnly _resourceFileMetadata As List(Of DictionaryEntry)

        'The root component for the resource editor.  Cannot be Nothing.
        Private ReadOnly _rootComponent As ResourceEditorRootComponent

        'A pointer to the task provider service.  Gives us access to the VS task list.
        Private _errorListProvider As ErrorListProvider

        'The main thread we're running on.  Used just to verify that idle time processing
        '  is always on the main thread.
        Private ReadOnly _mainThread As System.Threading.Thread

        'Holds a set of tasks for each Resource that has any task list entries.
        Private ReadOnly _resourceTaskSets As New Dictionary(Of Resource, ResourceTaskSet)

        'A set of resources that need to be checked for errors during idle-time
        '  processing.
        Private ReadOnly _resourcesToDelayCheckForErrors As New HashSet(Of Resource)

        ' Indicate whether we should suspend delay checking temporary...
        Private _delayCheckSuspended As Boolean

        'True iff we're in the middle of adding or removing a Resource through AddResource or RemoveResource.  If not,
        '  and we get notified of an add by component changing service, it means an external source has added/removed
        '  the resource (i.e., Undo/Redo).
        Private _addingRemovingResourcesInternally As Boolean

        'The base path to use for resolving relative paths in the resx file.  This should be the
        '  directory where the resx file lives.
        Private ReadOnly _basePath As String

        ' We get ResourceWrite from this environment service
        '  the reason is some projects (device project) need write the resource file in v1.x format, but other projects write in 2.0 format.
        Private ReadOnly _resxService As IResXResourceService

        'True if the original file bases on alphabetized order, we will keep this style...
        Private _alphabetizedOrder As Boolean = True

        'The service provider provided by the designer host
        Private ReadOnly _serviceProvider As IServiceProvider

        ' Asynchronous flush & run custom tool already posted?
        Private _delayFlushAndRunCustomToolQueued As Boolean

        ' It is true, when we are loading a new file.
        '  CONSIDER: Some behaviors in the designer are different when we are loading the file. For example, we don't dirty the file, adding undo/redo...
        '  We should consider to make it to be a part of the global state of the designer, but not within one object.
        Private _isLoadingResourceFile As Boolean

        ' If it is true, we are adding a collection of resources to the file
        Private _inBatchAdding As Boolean

        ' Despite the name, the MultiTargetService does not support projects that target
        ' multiple frameworks. Rather, it is meant to support projects targeting an older
        ' version of the .NET Framework than the one in use by VS, and a large part of its
        ' job is to translate the .NET Framework 4.x types known to VS into .NET Framework
        ' 2.x/3.x types to be persisted into the .resx file for use by the application at
        ' run time. It largely assumes that VS is running on the newest .NET Framework,
        ' and thus will inherently understand (thanks to type forwarding) any 2.x/3.x
        ' types it comes across.
        ' This completely falls over when the project is targeting anything newer than
        ' .NET Framework 4.x. The service will translate the Framework types known to VS
        ' into equivalent .NET Core type, the designer will persist those in the .resx
        ' file, and then promptly fail when reading them back. Instead, we should use and
        ' persist the "native" types, on the assumption that they will be understood by
        ' the .NET Core process at run time.
        ' We will need to revisit this if/when VS moves to run on .NET Core.
        Private ReadOnly _multiTargetService As MultiTargetService
        Private ReadOnly _useCurrentProcessFrameworkForTypes As Boolean = False

        Private ReadOnly _allowMOTW As Boolean
#End Region

#Region "Constructors/Destructors"

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="RootComponent">The root component for this ResourceFile</param>
        ''' <param name="ServiceProvider">The service provider provided by the designer host</param>
        ''' <param name="BasePath">The base path to use for resolving relative paths in the resx file.</param>
        Public Sub New(mtsrv As MultiTargetService, RootComponent As ResourceEditorRootComponent, ServiceProvider As IServiceProvider, BasePath As String)
            Debug.Assert(RootComponent IsNot Nothing)
            Debug.Assert(ServiceProvider IsNot Nothing)

            _resources = New Dictionary(Of String, Resource)(StringComparers.ResourceNames)
            _resourceFileMetadata = New List(Of DictionaryEntry)

            _rootComponent = RootComponent
            _serviceProvider = ServiceProvider

            _basePath = BasePath

            _componentChangeService = DirectCast(ServiceProvider.GetService(GetType(IComponentChangeService)), IComponentChangeService)
            If ComponentChangeService Is Nothing Then
                Throw New Package.InternalException
            End If

            _multiTargetService = mtsrv

            Dim hierarchy As IVsHierarchy = DirectCast(ServiceProvider.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            If hierarchy IsNot Nothing Then
                Dim project As IVsProject = DirectCast(hierarchy, IVsProject)
                Dim sp As OLE.Interop.IServiceProvider = Nothing

                Dim hr As Integer = project.GetItemContext(VSITEMID.ROOT, sp) '0xFFFFFFFE VSITEMID_ROOT
                If Interop.NativeMethods.Succeeded(hr) Then
                    Dim pUnk As IntPtr
                    Dim g As Guid = GetType(IResXResourceService).GUID
                    Dim g2 As Guid = New Guid("00000000-0000-0000-C000-000000000046") 'IUnKnown
                    hr = sp.QueryService(g, g2, pUnk)
                    If Interop.NativeMethods.Succeeded(hr) AndAlso Not pUnk = IntPtr.Zero Then
                        _resxService = DirectCast(System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(pUnk), IResXResourceService)
                        System.Runtime.InteropServices.Marshal.Release(pUnk)
                    End If
                End If

                ' If we're in the context of a .NET project using the CPS-based project system
                ' then use VS types rather than project types on the assumption that the project
                ' is .NET Core-based.
                If hierarchy.IsCapabilityMatch("CPS & .NET") Then
                    Dim featureFlags = ServiceProvider.GetService(Of SVsFeatureFlags, IVsFeatureFlags)(throwOnFailure:=False)
                    If featureFlags IsNot Nothing Then
                        _useCurrentProcessFrameworkForTypes = featureFlags.IsFeatureEnabled("ResourceDesigner.UseImprovedTypeResolution", defaultValue:=False)
                    End If
                End If
            End If

            _mainThread = System.Threading.Thread.CurrentThread

            Try
                Dim allowUntrustedFiles As Object = Registry.GetValue("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\SDK", "AllowProcessOfUntrustedResourceFiles", Nothing)
                Dim untrustedFiles = TryCast(allowUntrustedFiles, String)
                If untrustedFiles IsNot Nothing Then
                    _allowMOTW = untrustedFiles.Equals("true", StringComparison.OrdinalIgnoreCase)
                End If
            Catch ex As Exception
                ' Deliberately empty
            End Try
        End Sub

        ''' <summary>
        ''' IDisposable.Dispose()
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub

        ''' <summary>
        ''' Dispose.
        ''' </summary>
        ''' <param name="Disposing">If True, we're disposing.  If false, we're finalizing.</param>
        Protected Sub Dispose(Disposing As Boolean)
            If Disposing Then
                'Stop listening to component removing events - we want to just tear down in peace.
                ComponentChangeService = Nothing

                'Stop delay-checking resources and remove ourselves from idle-time processing (very important)
                StopDelayingCheckingForErrors()

                'Remove all task list entries
                If _errorListProvider IsNot Nothing Then
                    _errorListProvider.Tasks.Clear()
                End If

                If _resources IsNot Nothing Then
                    'Note: The designer host disposing any Resources of ours that have been
                    '  added as components.  However, we do it now anyway, in case there are some
                    '  that didn't make into into the host container, or in case we later do the
                    '  optimization of delay-adding Resources as components.  The second dispose
                    '  won't hurt the Resource.
                    For Each resource In _resources.Values
                        resource.Dispose()
                    Next

                    _resources.Clear()
                End If

                If _resourceFileMetadata IsNot Nothing Then
                    _resourceFileMetadata.Clear()
                End If
            End If
        End Sub

#End Region

#Region "Properties"

        ''' <summary>
        ''' The service provider provided by the designer host
        ''' </summary>
        Public ReadOnly Property ServiceProvider As IServiceProvider
            Get
                Return _serviceProvider
            End Get
        End Property

        ''' <summary>
        ''' Returns/gets the ComponentChangeService used by this ResourceFile.  To have this class stop listening to
        '''   change events, set this property to Nothing.  It does not need to be set up initially - it gets it
        '''   automatically from the service provider passed in.
        ''' </summary>
        Public Property ComponentChangeService As IComponentChangeService
            Get
                Return _componentChangeService
            End Get
            Set
                _componentChangeService = Value
            End Set
        End Property

        ''' <summary>
        ''' Gets the ResourceEditorView associated with this ResourceFile.
        ''' </summary>
        ''' <remarks>Overridable for unit testing.</remarks>
        Public Overridable ReadOnly Property View As ResourceEditorView
            Get
                Return RootComponent.RootDesigner.GetView()
            End Get
        End Property

        ''' <summary>
        ''' Gets the root component associated with this resource file.
        ''' </summary>
        Public ReadOnly Property RootComponent As ResourceEditorRootComponent
            Get
                Return _rootComponent
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the designer host for the resource editor
        ''' </summary>
        Private ReadOnly Property DesignerHost As IDesignerHost
            Get
                If RootComponent.RootDesigner Is Nothing Then
                    Debug.Fail("No root designer")
                    Throw New Package.InternalException
                End If

                Dim Host As IDesignerHost = RootComponent.RootDesigner.DesignerHost
                Debug.Assert(Host IsNot Nothing)
                Return Host
            End Get
        End Property

        ''' <summary>
        ''' Returns the resources from this resource file
        ''' </summary>
        Friend ReadOnly Property Resources As Dictionary(Of String, Resource)
            Get
                Return _resources
            End Get
        End Property

        ''' <summary>
        ''' The base path to use for resolving relative paths in the resx file.  This should be the
        '''   directory where the resx file lives.
        ''' </summary>
        Public ReadOnly Property BasePath As String
            Get
                Return _basePath
            End Get
        End Property

        ''' <summary>
        '''  Get the taskProvider
        '''   directory where the resx file lives.
        ''' </summary>
        Private ReadOnly Property ErrorListProvider As ErrorListProvider
            Get
                If _errorListProvider Is Nothing Then
                    If RootComponent.RootDesigner IsNot Nothing Then
                        _errorListProvider = RootComponent.RootDesigner.GetErrorListProvider()
                    End If
                    Debug.Assert(_errorListProvider IsNot Nothing, "ErrorListProvider can not be found")
                End If
                Return _errorListProvider
            End Get
        End Property

        ''' <summary>
        '''  Whether the resource item belongs to a device project
        ''' </summary>
        Public ReadOnly Property IsInsideDeviceProject As Boolean Implements ResourceTypeEditor.IResourceContentFile.IsInsideDeviceProject
            Get
                Return RootComponent IsNot Nothing AndAlso RootComponent.IsInsideDeviceProject()
            End Get
        End Property

        ''' <summary>
        ''' Returns whether the provided type is supported in the project containing this resource file
        ''' </summary>
        Public Function IsSupportedType(Type As Type) As Boolean Implements ResourceTypeEditor.IResourceContentFile.IsSupportedType

            ' The type is considered supported unless the MultiTargetService says otherwise (MultiTargetService checks
            ' in the project's target framework).

            If _multiTargetService IsNot Nothing Then
                Return _multiTargetService.IsSupportedType(Type)
            Else
                Return True
            End If
        End Function

#End Region

#Region "Resource Naming and look-up"

        ''' <summary>
        ''' Gets a suggested name for a new Resource which is not used by any resource currently in this ResourceFile.
        ''' </summary>
        Public Function GetUniqueName(TypeEditor As ResourceTypeEditor) As String
            Dim UniqueNamePrefix As String

            Try
                UniqueNamePrefix = TypeEditor.GetSuggestedNamePrefix().Trim()
                If UniqueNamePrefix = "" OrElse UniqueNamePrefix.IndexOf(" "c) >= 0 Then
                    Debug.Fail("Bad unique name prefix - localization bug?")
                    UniqueNamePrefix = ""
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(GetUniqueName), NameOf(ResourceFile))
                UniqueNamePrefix = ""
            End Try

            If UniqueNamePrefix = "" Then
                'Use a default prefix if there's trouble
                UniqueNamePrefix = "id"
            End If

            Dim UniqueNameFormat As String = UniqueNamePrefix & "{0:0}"
            Return GetUniqueName(UniqueNameFormat)
        End Function

        ''' <summary>
        ''' Gets a suggested name for a new Resource which is not used by any resource currently in this ResourceFile.
        ''' </summary>
        ''' <param name="NameFormat">A format to use for String.Format which indicates how to format the integer portion of the name.  Must contain a single {0} parameter.</param>
        Public Function GetUniqueName(NameFormat As String) As String
            Debug.Assert(NameFormat.IndexOf("{") >= 0 AndAlso NameFormat.IndexOf("}") >= 2,
                "NameFormat must contain a replacement arg")

            Dim SuffixInteger As Integer = 1
            Do
                Dim NewName As String = String.Format(NameFormat, SuffixInteger)
                If Not Contains(NewName) Then
                    Return NewName
                End If

                SuffixInteger += 1
            Loop
        End Function

        ''' <summary>
        ''' Determines if a resource with a given name (case-insensitive) exists in this ResourceFile.
        ''' </summary>
        ''' <param name="Name">The resource name to look for (case insensitive)</param>
        Public Function Contains(Name As String) As Boolean
            Return FindResource(Name) IsNot Nothing
        End Function

        ''' <summary>
        ''' Determines if a particular resource is in this ResourceFile (by reference)
        ''' </summary>
        ''' <param name="Resource"></param>
        Public Function Contains(Resource As Resource) As Boolean
            Return _resources.ContainsValue(Resource)
        End Function

        ''' <summary>
        ''' Searches for a resource with a given name (case-insensitive) in this ResourceFile.
        ''' </summary>
        ''' <param name="Name">The resource name to look for (case insensitive)</param>
        ''' <returns>The found Resource, or Nothing if not found.</returns>
        Public Function FindResource(Name As String) As Resource
            If Name = "" Then
                Return Nothing
            End If

            Dim Resource As Resource = Nothing
            If _resources.TryGetValue(Name, Resource) Then
                Debug.Assert(Resource.ParentResourceFile Is Me)
            End If

            Return Resource
        End Function

        ''' <summary>
        ''' Searches for a resource with a given file link (case-insensitive) in this ResourceFile.
        ''' </summary>
        ''' <param name="FileFullPath">The full path name of the linked file to look for (case insensitive)</param>
        ''' <returns>The found Resource, or Nothing if not found.</returns>
        ''' <remarks> We should be careful there, because there could be different path pointing to a same file</remarks>
        Public Function FindLinkResource(FileFullPath As String) As Resource
            Dim fileInfo As New FileInfo(FileFullPath)
            FileFullPath = fileInfo.FullName
            For Each Resource In _resources.Values
                If Resource.IsLink Then
                    Dim linkFileInfo As New FileInfo(Resource.AbsoluteLinkPathAndFileName)
                    If String.Equals(FileFullPath, linkFileInfo.FullName, StringComparison.OrdinalIgnoreCase) Then
                        Return Resource
                    End If
                End If
            Next
            Return Nothing
        End Function

#End Region

#Region "Adding/removing/renaming resources"

        ''' <summary>
        '''  Add a collection of resources to the ResourceFile
        ''' </summary>
        ''' <param name="NewResources">A collection of resource items to add</param>
        Public Sub AddResources(NewResources As ICollection(Of Resource))
            Debug.Assert(NewResources IsNot Nothing, "Invalid Resources collection")

            _inBatchAdding = True
            Try
                For Each Resource In NewResources
                    AddResource(Resource)
                Next
                If Not _isLoadingResourceFile Then
                    AddNecessaryReferenceToProject(NewResources)
                End If
            Finally
                _inBatchAdding = False
            End Try
        End Sub

        ''' <summary>
        ''' Adds a new Resource to the ResourceFile.
        ''' </summary>
        ''' <param name="NewResource">The Resource to add.  Must not be blank.</param>
        ''' <remarks>Exception throw if the name is not unique.</remarks>
        Public Sub AddResource(NewResource As Resource)
            If NewResource.Name = "" Then
                Debug.Fail("Resource Name is blank - we shouldn't reach here with that condition")
                Throw NewException(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_NameBlank, HelpIDs.Err_NameBlank)
            End If
            If Contains(NewResource.Name) Then
                Throw NewException(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_DuplicateName_1Arg, NewResource.Name), HelpIDs.Err_DuplicateName)
            End If

            'Set up a type resolution context for the resource in case this hasn't
            '  already been done (won't be done if the Resource was deserialized
            '  during an Undo/Redo or Drop/Paste operation, for example)
            NewResource.SetTypeResolutionContext(View)

#If DEBUG Then
            Dim ResourcesCountOld As Integer = _resources.Count
#End If

            Dim AddingRemovingResourcesInternallySave As Boolean = _addingRemovingResourcesInternally
            Try
                _addingRemovingResourcesInternally = True

                'Add the component to our designer's container.
                'This will cause us to get notified via ComponentChangeService.ComponentAdded, which is where we
                '  will actually add the Resource to our internal list.
                DesignerHost.Container.Add(NewResource, NewResource.Name)
            Finally
                _addingRemovingResourcesInternally = AddingRemovingResourcesInternallySave
            End Try

#If DEBUG Then
            Debug.Assert(_resources.Count = ResourcesCountOld + 1)
#End If

            If Not _isLoadingResourceFile Then
                ResourceEditorTelemetry.OnResourceAdded(NewResource.FriendlyValueTypeName)
            End If

        End Sub

        ''' <summary>
        ''' Removes the specified Resource from this ResourceFile.
        ''' </summary>
        ''' <param name="Resource">The Resource to remove.  Must exist in the ResourceFile</param>
        ''' <param name="DisposeResource">If True, the Resource is also disposed.</param>
        Public Sub RemoveResource(Resource As Resource, DisposeResource As Boolean)
            Debug.Assert(Resource IsNot Nothing)
            Debug.Assert(FindResource(Resource.Name) Is Resource, "RemoveResource: not found by Name")
            Debug.Assert(_resources.ContainsValue(Resource), "RemoveResource: not found")

            Dim ResourcesCountOld As Integer = _resources.Count
            Dim AddingRemovingResourcesInternallySave As Boolean = _addingRemovingResourcesInternally

            Try
                _addingRemovingResourcesInternally = True

                'Remove the component from our designer's container.
                'This will cause us to get notified via ComponentChangeService.ComponentRemoved, which is where we
                '  will actually remove the Resource from our internal list
                DesignerHost.Container.Remove(Resource)
            Finally
                _addingRemovingResourcesInternally = AddingRemovingResourcesInternallySave
            End Try

            Debug.Assert(_resources.Count = ResourcesCountOld - 1)

            'Remove any task list entries for this resource
            ClearResourceTasks(Resource)

            If DisposeResource Then
                Resource.Dispose()
            End If

            ResourceEditorTelemetry.OnResourceRemoved(Resource.FriendlyValueTypeName)

        End Sub

        ''' <summary>
        ''' Called by the component change service when a new component is added to the designer host's container.
        ''' We get notified of this for both our own internal adding/removing and also for those done on our behalf
        '''   by Undo/Redo.
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        ''' <remarks>
        ''' Here we do the actual adding of the resource to our list.
        ''' </remarks>
        Private Sub ComponentChangeService_ComponentAdded(sender As Object, e As ComponentEventArgs) Handles _componentChangeService.ComponentAdded
            Dim ResourceObject As Object = e.Component
            If TypeOf ResourceObject IsNot Resource Then
                Debug.Fail("How could we be adding a component that's not a Resource?")
                Exit Sub
            End If

            Dim Resource As Resource = DirectCast(e.Component, Resource)
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be Nothing")
                Exit Sub
            End If

            'First thing, set the type resolution context (might not have been done yet if this component add was
            '  through a Undo/Redo operation)
            Resource.SetTypeResolutionContext(View)

            Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: Adding " & Resource.ToString())

            Debug.Assert(FindResource(Resource.Name) IsNot Resource, "already a resource by that name")
            Debug.Assert(Not _resources.ContainsValue(Resource), "already exists in our list")

            'Add it to our list (upper-case the key to normalize for in-case-sensitive look-ups)
            _resources.Add(Resource.Name, Resource)

            'Set the parent
            Resource.ParentResourceFile = Me

            'Notify the Find feature
            RootComponent.RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=True)

            'Update the number of resources in this resource's category
            Dim Category As Category = Resource.GetCategory(View.Categories)
            If Category IsNot Nothing Then
                Category.ResourceCount += 1
            Else
                Debug.Fail("Couldn't find category for resource")
            End If

            'Add to our list of resources to check for errors in idle time
            DelayCheckResourceForErrors(Resource)

            'Notify the view that resources have been added (if they were added by someone besides us, think "Undo/Redo")
            If Not _addingRemovingResourcesInternally Then
                Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: (Resource was added externally)")
                View.OnResourceAddedExternally(Resource)
            End If

            ' Add Reference to the project system if necessary.
            ' Note: we need do this what ever it is added by an editing or undoing/redoing, but never when we are loading the file.
            If Not _isLoadingResourceFile AndAlso Not _inBatchAdding Then
                AddNecessaryReferenceToProject(New Resource() {Resource})
            End If

            'Set up a file watcher for this resource if it's a link
            If View IsNot Nothing Then
                Resource.AddFileWatcherEntry(View.FileWatcher)
            End If
        End Sub

        ''' <summary>
        ''' Called by the component change service when a Resource is removed, either by us or by an external
        '''   party (Undo/Redo).
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        Private Sub ComponentChangeService_ComponentRemoved(sender As Object, e As ComponentEventArgs) Handles _componentChangeService.ComponentRemoved
            Dim ResourceObject As Object = e.Component
            If TypeOf ResourceObject IsNot Resource Then
                Debug.Assert(TypeOf ResourceObject Is ResourceEditorRootComponent, "How could we be removing a component that's not a Resource?")
                Exit Sub
            End If

            Dim Resource As Resource = DirectCast(e.Component, Resource)
            If Resource Is Nothing Then
                Debug.Fail("Resource shouldn't be Nothing")
                Exit Sub
            End If

            Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: Removing " & Resource.ToString())

            Debug.Assert(FindResource(Resource.Name) Is Resource, "not found by Name")
            Debug.Assert(_resources.ContainsValue(Resource), "not found")

            'Go ahead and remove from our list (keys are normalized as upper-case)
            _resources.Remove(Resource.Name)

            'Remove the parent pointer
            Resource.ParentResourceFile = Nothing

            'Notify Find
            RootComponent.RootDesigner.InvalidateFindLoop(ResourcesAddedOrRemoved:=True)

            'Update the number of resources in this resource's category
            Dim Category As Category = Resource.GetCategory(View.Categories)
            If Category IsNot Nothing Then
                Category.ResourceCount -= 1
            Else
                Debug.Fail("Couldn't find category for resource")
            End If

            'Clear any task list entries
            ClearResourceTasks(Resource)

            'If this Resource is slated to be checked for errors at idle time, that's no longer necessary.
            RemoveResourceToDelayCheckForErrors(Resource)

            'Notify the view that resources have been removed (if they were removed by someone besides us, think "Undo/Redo")
            If Not _addingRemovingResourcesInternally Then
                Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: (Resource was removed externally)")
                View.OnResourceRemovedExternally(Resource)
            End If

            'Remove the file watcher for this resource if it's a link (it won't be able to when the Undo/Redo engine disposes it, because
            '  it won't have a parent resource file then and can't get to the file watcher.
            If View IsNot Nothing Then
                Resource.RemoveFileWatcherEntry(View.FileWatcher)
            End If

            '
            ' Whenever we delete a resource, we have to make sure that we run our custom tool. Not doing so may cause problems if:
            ' step 1) Delete resource "A"
            ' step 2) Rename resource "B" to A
            ' 
            ' Now the CodeModel gets angry because we haven't flushed the contents of the designer between step 1 & 2, which means that
            ' both the properties are still in the generated code, and we'd end up with code resource "A" if the operation were to succeed.
            ' 
            ' Flushing & running the SFG after deletes will take care of this scenario... We also take care and post the message so that if
            ' we deleted multiple settings, we only do this once (perf)
            '
            DelayFlushAndRunCustomTool()
        End Sub

        ''' <summary>
        ''' Rename a resource in the ResourceFile.  This operation must come through here and not simply
        '''   be done directly on the Resource, because we also have to change the Resource's
        '''   ISite's name.
        ''' </summary>
        ''' <param name="Resource">Resource to rename</param>
        ''' <param name="NewName">New name.  If it's not unique, an exception is thrown.</param>
        ''' <remarks>Caller is responsible for showing error message boxes</remarks>
        Public Sub RenameResource(Resource As Resource, NewName As String)
            If Contains(Resource) Then
                Debug.Assert(DesignerHost.Container.Components(Resource.Name) IsNot Nothing)

                If Resource.Name.Equals(NewName, StringComparison.Ordinal) Then
                    'Name didn't change - nothing to do
                    Exit Sub
                End If

                'Verify that the new name is unique.  Note that it's okay to rename in such a
                '  way that only the case of the name changes (thus ExistingResource will be
                '  the same as Resource, since we find case-insensitively).
                Dim ExistingResource As Resource = FindResource(NewName)
                If ExistingResource IsNot Nothing AndAlso ExistingResource IsNot Resource Then
                    Throw NewException(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_DuplicateName_1Arg, NewName), HelpIDs.Err_DuplicateName)
                End If

                'Make sure the resx file is checked out if it isn't yet.  Otherwise this failure might
                '  happen after we've already changed our internal name but before the site's name gets
                '  changed (because we change our internal name in response to listening in on 
                '  ComponentChangeService).
                View.RootDesigner.DesignerLoader.ManualCheckOut()

                'Rename the component's site's name.  This will cause a ComponentChangeService.ComponentRename event,
                '  which we listen to, and from which we will change the Resource's name.  (We need to do it
                '  this way, because Undo/Redo on a name will change the component's site's name only, and we
                '  need to pick up on those changes in order to reflect the change in the Resource itself.
                Resource.IComponent_Site.Name = NewName

                ResourceEditorTelemetry.OnResourceRenamed(Resource.FriendlyValueTypeName)

            Else
                Debug.Fail("Trying to rename component that's not in the resource file")
            End If
        End Sub

        ''' <summary>
        ''' Called by the component change service when a resource has been renamed (rather, its component
        '''   ISite has been renamed).  We need to keep these in sync.
        ''' This is called both when we rename the Resource ourselves and when something external does it
        '''   (Undo/Redo).
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        Private Sub ComponentChangeService_ComponentRename(sender As Object, e As ComponentRenameEventArgs) Handles _componentChangeService.ComponentRename
            If TypeOf e.Component IsNot Resource Then
                Debug.Fail("Got component rename event for a component that isn't a resource")
                Exit Sub
            End If

            Dim Resource As Resource = DirectCast(e.Component, Resource)
            Debug.Assert(e.OldName.Equals(Resource.Name, StringComparison.Ordinal))

            Debug.WriteLineIf(Switches.RSEAddRemoveResources.TraceVerbose, "Add/Remove Resources: Renaming " & Resource.ToString() & " to """ & e.NewName & """")

            If Not Contains(Resource) Then
                Debug.Fail("Trying to rename component that's not in the resource file")
                Exit Sub
            End If

            If Resource.Name.Equals(e.NewName, StringComparison.OrdinalIgnoreCase) Then
                'The name hasn't changed (or differs only by case) - okay to rename
            ElseIf Not Contains(e.NewName) Then
                'The new name is not in use by any current resource - okay to rename
            Else
                'Whoops.  Something's wrong.
                Debug.Fail("Got a RenameComponent event to a name that's already in use - shouldn't have happened")
                Throw CreateArgumentException(NameOf(e.NewName))
            End If

            'Go ahead and make the change
            Debug.Assert(Resource.ValidateName(e.NewName, Resource.Name), "Component's Site's name was changed to an invalid name.  That shouldn't have happened.")

            Dim OldName As String = Resource.Name

            'Since resources in the ResourceFile are placed in the dictionary using the Name as key, if
            '  we change the Name, the location of the resource in the dictionary will not longer be correct
            '  (because we just changed the key, which it uses to search the dictionary).  So we have to
            '  remove ourself first and then re-insert ourself into the dictionary.
            _resources.Remove(Resource.Name)
            Try
                Resource.NameRawWithoutUndo = e.NewName
            Catch ex As Exception When ReportWithoutCrash(ex, "Unexpected error changing the name of the resource", NameOf(ResourceFile))
            End Try
            _resources.Add(Resource.Name, Resource)

            'Notify the view that resources have been removed (if they were removed by someone besides us, think "Undo/Redo")
            View.OnResourceTouched(Resource)

            'Fix up the project to use the new name, if we're creating strongly typed resource classes            
            Try
                View.CallGlobalRename(OldName, e.NewName)
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ComponentChangeService_ComponentRename), NameOf(ResourceFile))
                RootComponent.RootDesigner.GetView().DsMsgBox(ex)
            End Try
        End Sub

        ''' <summary>
        ''' Called by the component change service when a resource has been changed 
        ''' This is called both when we changed the Resource ourselves and when something external does it
        '''   (Undo/Redo).
        ''' </summary>
        ''' <param name="sender">Event sender</param>
        ''' <param name="e">Event args</param>
        Private Sub ComponentChangeService_ComponentChanged(sender As Object, e As ComponentChangedEventArgs) Handles _componentChangeService.ComponentChanged
            If TypeOf e.Component IsNot Resource Then
                Debug.Fail("Got component change event for a component that isn't a resource")
                Exit Sub
            End If

            Dim resource = DirectCast(e.Component, Resource)

            View.OnResourceTouched(resource)

            ResourceEditorTelemetry.OnResourceChanged(resource.FriendlyValueTypeName)

        End Sub

#End Region

#Region "Reading/Writing/Enumerating"

        ''' <summary>
        ''' Reads resources from a string (contents of a resx file).
        ''' </summary>
        ''' <param name="allBufferText">The TextReader to read from</param>
        Public Sub ReadResources(resourceFileName As String, allBufferText As String)

            If IsDangerous(resourceFileName, allBufferText) Then
                View.DsMsgBox(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.BlockedResx, resourceFileName), MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Using TextReader = New StringReader(allBufferText)
                Dim ResXReader As ResXResourceReader
                Dim TypeResolutionService As ITypeResolutionService = View.GetTypeResolutionService()
                If TypeResolutionService IsNot Nothing Then
                    ResXReader = New ResXResourceReader(TextReader, TypeResolutionService)
                Else
                    ResXReader = New ResXResourceReader(TextReader, ResourceEditorView.GetDefaultAssemblyReferences())
                End If
                ResXReader.BasePath = _basePath

                ReadResources(ResXReader)
                ResXReader.Close()
            End Using
        End Sub

        Public Function TypeNameConverter(runtimeType As Type) As String
            Debug.Assert(runtimeType IsNot Nothing, "runtimeType cannot be Nothing!")

            If _useCurrentProcessFrameworkForTypes OrElse
                _multiTargetService Is Nothing Then
                Return runtimeType.AssemblyQualifiedName
            Else
                Return _multiTargetService.TypeNameConverter(runtimeType)
            End If
        End Function

        ''' <summary>
        ''' Writes all resources into a TextWriter in resx format.
        ''' </summary>
        ''' <param name="TextWriter">TextWriter to write to</param>
        Public Sub WriteResources(TextWriter As TextWriter)
            Dim ResXWriter As IResourceWriter

            If _resxService IsNot Nothing Then
                ResXWriter = _resxService.GetResXResourceWriter(TextWriter, _basePath)
            Else
                Dim r As New ResXResourceWriter(TextWriter, AddressOf TypeNameConverter) With {
                    .BasePath = _basePath
                }

                ResXWriter = r
            End If

            'This call will generate the resources.  We don't want to close the ResXWriter because it
            '  will also close the TextWriter, which closes its stream, which may not be expected by
            '  the caller.
            WriteResources(ResXWriter)
        End Sub

        ''' <summary>
        ''' Reads all resources into this ResourceFile from a ResXReader
        ''' </summary>
        ''' <param name="ResXReader">The ResXReader to read from</param>
        Private Sub ReadResources(ResXReader As ResXResourceReader)
            Debug.Assert(ResXReader IsNot Nothing, "ResXReader must exist!")

            _isLoadingResourceFile = True

            Try
                Dim orderID As Integer = 0
                Dim lastName As String = String.Empty

                _resources.Clear()
                _resourceFileMetadata.Clear()

                ResXReader.UseResXDataNodes = True
                Using New WaitCursor
                    For Each DictEntry As DictionaryEntry In ResXReader
                        Dim Node As ResXDataNode = DirectCast(DictEntry.Value, ResXDataNode)
                        Dim Resource As Resource = Nothing

                        Try
                            Resource = New Resource(Me, Node, orderID, View)
                            orderID += 1

                            'If duplicate Names are found, this function will throw an exception (which is what we want - it will keep the
                            '  file from loading)
                            AddResource(Resource)

                            ' we check whether the resource item in the original file was alphabetized, we keep the style when we save it...
                            If _alphabetizedOrder Then
                                If StringComparers.ResourceNames.Compare(lastName, Resource.Name) > 0 Then
                                    _alphabetizedOrder = False
                                Else
                                    lastName = Resource.Name
                                End If
                            End If
                        Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ReadResources), NameOf(ResourceFile))
                            If Resource IsNot Nothing Then
                                Resource.Dispose()
                            End If
                            Throw
                        End Try
                    Next
                End Using

                ' Read and save meta data
                Dim enumerator As IDictionaryEnumerator = ResXReader.GetMetadataEnumerator()
                If enumerator IsNot Nothing Then
                    While enumerator.MoveNext()
                        _resourceFileMetadata.Add(enumerator.Entry)
                    End While
                End If
            Finally
                _isLoadingResourceFile = False

                ResourceEditorTelemetry.OnResourcesLoaded(_resources, _resourceFileMetadata.Count)
            End Try

        End Sub

        ''' <summary>
        ''' Writes all resources into a ResXResourceWriter
        ''' </summary>
        ''' <param name="ResXWriter">The ResXResourceWriter instance to use</param>
        Private Sub WriteResources(ResXWriter As IResourceWriter)
            Debug.Assert(ResXWriter IsNot Nothing, "ResXWriter must exist.")

            If _resources IsNot Nothing Then
                ' NOTE: We save all meta data first...  We don't have a way maintain the right order between Meta data items and resource items today.
                ' Keep all meta data items if it is possible...
                If _resourceFileMetadata IsNot Nothing AndAlso _resourceFileMetadata.Count > 0 Then
                    Dim NewWriter As ResXResourceWriter = TryCast(ResXWriter, ResXResourceWriter)
                    If NewWriter IsNot Nothing Then
                        For Each entry In _resourceFileMetadata
                            NewWriter.AddMetadata(CStr(entry.Key), entry.Value)
                        Next
                    End If
                End If

                If _resources.Count > 0 Then
                    Dim resourceList As Resource() = New Resource(_resources.Count - 1) {}
                    Dim i As Integer = 0
                    For Each Resource In _resources.Values
                        resourceList(i) = Resource
                        i += 1
                    Next

                    Dim comparer As IComparer
                    If _alphabetizedOrder Then
                        comparer = AlphabetizedOrderComparer.Instance
                    Else
                        comparer = OriginalOrderComparer.Instance
                    End If

                    Array.Sort(resourceList, comparer)

                    Dim failedList As String = Nothing
                    Dim extraMessage As String = Nothing

                    For i = 0 To resourceList.Length - 1
                        Dim resource As Resource = resourceList(i)
                        Try
                            ResXWriter.AddResource(resource.ResXDataNode.Name, resourceList(i).ResXDataNode)
                        Catch ex As Exception When ReportWithoutCrash(ex, NameOf(WriteResources), NameOf(ResourceFile))
                            resource.SetTaskFromGetValueException(ex, ex)
                            If failedList IsNot Nothing Then
                                failedList = My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_NameList, failedList, resource.Name)
                            Else
                                failedList = My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_Name, resource.Name)
                                extraMessage = ex.Message
                            End If
                        End Try
                    Next

                    If failedList IsNot Nothing Then
                        RootComponent.RootDesigner.GetView().DsMsgBox(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_CantSaveResouce_1Arg, failedList) & vbCrLf & vbCrLf & extraMessage,
                            MessageBoxButtons.OK, MessageBoxIcon.Error, , HelpIDs.Err_CantSaveBadResouceItem)
                    End If
                End If

                ResXWriter.Generate()
            Else
                Throw New Exception("Must read resources before attempting to write")
            End If
        End Sub

#End Region

#Region "UI"

        ''' <summary>
        ''' Invalidates this resource in the resource editor view, which causes it to be updated on the next
        '''   paint.
        ''' </summary>
        ''' <param name="Resource">The resource to invalidate</param>
        ''' <param name="InvalidateThumbnail">If True, then the Resource's thumbnail is also invalidated so it will be regenerated on the next paint.</param>
        Public Sub InvalidateResourceInView(Resource As Resource, Optional InvalidateThumbnail As Boolean = False)
            If RootComponent.RootDesigner IsNot Nothing AndAlso RootComponent.RootDesigner.GetView() IsNot Nothing Then
                RootComponent.RootDesigner.GetView().InvalidateResource(Resource, InvalidateThumbnail)
            End If
        End Sub

#End Region

#Region "Task List integration"

#Region "ResourceTaskType enum"

        ''' <summary>
        ''' The types of errors which can occur for a Resource.
        ''' NOTE: These types are mutually exclusive.  I.e., each Resource is allowed to log
        '''   a single task list item for *each* of the values in this enum.  E.g., a resource
        '''   can have a task list item for bad link (CantInstantiateResource) and for
        '''   a bad Name, and both task items will show up in the task list.
        ''' </summary>
        Public Enum ResourceTaskType
            'ID is bad or otherwise not a good idea
            BadName

            'Unable to instantiate the resource (bad link, assembly not found, etc.)
            CantInstantiateResource

            'Comments in a form's resx file will be stripped by the form designer.
            CommentsNotSupportedInThisFile
        End Enum

#End Region

#Region "Nested class - ResourceTaskSet"

        ''' <summary>
        ''' A set of task list entries for a single Resource.  We create one of these whenever we
        '''   associate a task list entry with a Resource.  It has an array of tasks, which should 
        '''   be considered as a set of "slots" for task list entries.  Each slot is of a different
        '''   kind.  A resource can have a single task list entry for each slot, and thus for
        '''   each distinct kind of task list entry or error/warning.
        ''' </summary>
        Private NotInheritable Class ResourceTaskSet
            'The number of error types that we have.  Calculated from the ResourceTaskType enum.
            Private Shared ReadOnly s_errorTypeCount As Integer

            'Backs Tasks property
            '  (could just have well been a hashtable as an array, but an array is more lightweight)
            Private ReadOnly _tasks() As ResourceTask

            ''' <summary>
            ''' Shared sub New.  Calculates m_ErrorTypeCount and verifies that the
            '''   enum types start with zero and are contiguous.  This is necessary in order
            '''   to use them as an index into a simple array.
            ''' </summary>
            Shared Sub New()
                s_errorTypeCount = [Enum].GetValues(GetType(ResourceTaskType)).Length

#If DEBUG Then
                'Verify that the enums start with zero and are contiguous
                For Index As Integer = 0 To s_errorTypeCount - 1
                    Debug.Assert(CInt([Enum].GetValues(GetType(ResourceTaskType)).GetValue(Index)) = Index,
                        "The values in ResourceErrorType must start at 0 and be contiguous")
                Next
#End If
            End Sub

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            Public Sub New()
                ReDim _tasks(s_errorTypeCount - 1)
            End Sub

            ''' <summary>
            ''' Gets the array (indexed by ResourceTaskType) of tasks in this set
            ''' </summary>
            Public ReadOnly Property Tasks As ResourceTask()
                Get
                    Return _tasks
                End Get
            End Property

        End Class

#End Region

#Region "Nested class - ResourceTask"

        ''' <summary>
        ''' A single task list entry for resources.  Contains a pointer back to the Resource
        '''   that is associated with it, for handling navigation (when the user double-clicks
        '''   on a task list entry).
        ''' </summary>
        Friend NotInheritable Class ResourceTask
            Inherits ErrorTask

            'The resource associated with this task list entry.
            Private ReadOnly _resource As Resource

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <param name="Resource"></param>
            Public Sub New(Resource As Resource)
                _resource = Resource
            End Sub

            ''' <summary>
            ''' The resource associated with this task list entry.
            ''' </summary>
            Public ReadOnly Property Resource As Resource
                Get
                    Return _resource
                End Get
            End Property

        End Class

#End Region

        ''' <summary>
        ''' Returns True iff the specified Resource has any task list items.
        ''' </summary>
        ''' <param name="resource">The resource to look for task entries for.</param>
        Public Function ResourceHasTasks(Resource As Resource) As Boolean
            Dim TaskSet As ResourceTaskSet = Nothing
            If Not _resourceTaskSets.TryGetValue(Resource, TaskSet) Then
                Return False
            End If

            'Check all task slots in the task set
            For i As Integer = 0 To TaskSet.Tasks.Length - 1
                If TaskSet.Tasks(i) IsNot Nothing Then
                    'We found a task.
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Gets the task entry text for a particular resource and resource type.  Returns Nothing if there is
        '''   no such task.
        ''' </summary>
        ''' <param name="Resource">The task to get the text for.</param>
        ''' <param name="TaskType">The type of task list entry to retrieve for this Resource.</param>
        Public Function GetResourceTaskMessage(Resource As Resource, TaskType As ResourceTaskType) As String
            Dim TaskSet As ResourceTaskSet = Nothing
            If Not _resourceTaskSets.TryGetValue(Resource, TaskSet) Then
                Return Nothing
            End If

            Dim Task As TaskListItem = TaskSet.Tasks(TaskType)
            If Task Is Nothing Then
                Return Nothing
            End If

            'Found an entry.
            Return Task.Text
        End Function

        ''' <summary>
        ''' Gets the text from all task list entries for a particular Resource, separated by
        '''   CR/LF.
        ''' </summary>
        ''' <param name="Resource">The resource to look up task list entries for.</param>
        Public Function GetResourceTaskMessages(Resource As Resource) As String
            Dim TaskSet As ResourceTaskSet = Nothing
            If Not _resourceTaskSets.TryGetValue(Resource, TaskSet) Then
                Return Nothing
            End If

            Dim Messages As String = ""
            For i As Integer = 0 To TaskSet.Tasks.Length - 1
                If TaskSet.Tasks(i) IsNot Nothing Then
                    If Messages <> "" Then
                        Messages &= vbCrLf
                    End If

                    Messages &= TaskSet.Tasks(i).Text
                End If
            Next

            Return Messages
        End Function

        ''' <summary>
        ''' This handler gets called when the user double-clicks on a task list entry.
        ''' </summary>
        ''' <param name="sender">The task that was double-clicked.</param>
        ''' <param name="e">Event args</param>
        Private Sub OnTaskNavigate(sender As Object, e As EventArgs)
            Dim Task As ResourceTask = TryCast(sender, ResourceTask)
            If Task Is Nothing Then
                Debug.Fail("Navigate sender not a resourcetask?")
                Exit Sub
            End If

            If Task.Resource IsNot Nothing Then
                View.NavigateToResource(Task.Resource)
            Else
                Debug.Fail("Task list entry didn't contain a resource reference")
            End If
        End Sub

        ''' <summary>
        ''' Associates a particular task list text with a given resource.  If there is already a task list
        '''   entry associated with this resource and resource task type, this new one takes its place.
        ''' </summary>
        ''' <param name="Resource">The Resource for which the new task list entry will apply.</param>
        ''' <param name="TaskType">The type of task list entry (type of error/warning)</param>
        ''' <param name="Text">The text of the new task list entry.</param>
        ''' <param name="Priority">The priority of the new task list entry.</param>
        ''' <param name="HelpLink">The help link of the new task list entry.</param>
        ''' <param name="ErrorCategory">The ErrorCategory of the new task list entry. It is an Error or Warning.</param>
        Public Sub SetResourceTask(Resource As Resource, TaskType As ResourceTaskType, Text As String, Priority As TaskPriority, HelpLink As String, ErrorCategory As TaskErrorCategory)
            Debug.Assert(Resource IsNot Nothing)
            Dim taskProvider As ErrorListProvider = ErrorListProvider
            If taskProvider IsNot Nothing Then
                'Get current task set for this resource.  If none, then create one.
                Dim TaskSet As ResourceTaskSet = Nothing
                If Not _resourceTaskSets.TryGetValue(Resource, TaskSet) Then
                    TaskSet = New ResourceTaskSet
                    _resourceTaskSets.Add(Resource, TaskSet)
                End If

                'Optimization: If the task already exists with the correct Text and Priority, there's no need
                '  to update the task list, we can just leave things as they are.
                Dim OldTask As ResourceTask = TaskSet.Tasks(TaskType)
                If OldTask IsNot Nothing Then
                    If OldTask.Text.Equals(Text, StringComparison.Ordinal) _
                        AndAlso OldTask.Priority = Priority _
                        AndAlso OldTask.Resource Is Resource _
                    Then
                        'The task is already there and set up properly, so there's nothing to do.
                        Exit Sub
                    Else
                        'Need to remove the old task
                        taskProvider.Tasks.Remove(OldTask)
                    End If
                End If

                'Create the new task and put it in the task set.
                Dim Task As New ResourceTask(Resource)
                TaskSet.Tasks(TaskType) = Task
                With Task
                    AddHandler .Navigate, AddressOf OnTaskNavigate 'This sets up navigation
                    .CanDelete = False
                    '.Category = TaskCategory.BuildCompile
                    .Checked = False
                    .Document = RootComponent.RootDesigner.GetResXFileNameAndPath()
                    .HelpKeyword = HelpLink
                    .IsCheckedEditable = False
                    .IsPriorityEditable = False
                    .IsTextEditable = False
                    .Priority = Priority
                    .ErrorCategory = ErrorCategory
                    .Text = Text
                End With

                'And to the task list, and get the task list to show so that the user is aware
                '  there are errors.
                taskProvider.Tasks.Add(Task)

                ' We want to bring up the error list window without activating the window... It is especially true because we do validation at Idle time.
                Dim vsUIShell As IVsUIShell = TryCast(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                If vsUIShell IsNot Nothing Then
                    Dim taskProviderToolWindowID As Guid = New Guid(EnvDTE80.WindowKinds.vsWindowKindErrorList)
                    Dim vsWindowFrame As IVsWindowFrame = Nothing
                    If VSErrorHandler.Succeeded(vsUIShell.FindToolWindow(CUInt(__VSFINDTOOLWIN.FTW_fForceCreate), taskProviderToolWindowID, vsWindowFrame)) Then
                        If vsWindowFrame IsNot Nothing Then
                            If VSErrorHandler.Failed(vsWindowFrame.ShowNoActivate()) Then
                                Debug.Fail("Why we failed to activate the error window")
                            End If
                        End If
                    End If
                Else
                    Debug.Fail("Why we can't find IVsUIShell service?")
                End If

                'We need to invalidate the resource to ensure that the error icon shows up next to
                '  it.
                Resource.InvalidateUI()
            End If
        End Sub

        ''' <summary>
        ''' Clear a the slot for a particular task type in a particular resource.
        ''' </summary>
        ''' <param name="Resource">The resource from which the task list entry will be cleared.</param>
        ''' <param name="TaskType">The type of task list entry to clear, if it exists.</param>
        Public Sub ClearResourceTask(Resource As Resource, TaskType As ResourceTaskType)
            Dim TaskSet As ResourceTaskSet = Nothing
            If Not _resourceTaskSets.TryGetValue(Resource, TaskSet) Then
                Exit Sub 'Nothing to clear
            End If

            Dim Task As TaskListItem = TaskSet.Tasks(TaskType)
            If Task IsNot Nothing Then
                'Remove the task for this task type, if it exists.
                If _errorListProvider IsNot Nothing Then
                    _errorListProvider.Tasks.Remove(Task)
                End If
                TaskSet.Tasks(TaskType) = Nothing

                'We need to invalidate the resource to ensure that the error icon next to it gets cleared.
                Resource.InvalidateUI()
            End If

            'If there are no more tasks for this Resource, we can remove the ResourceTaskSet
            '  entry from the hash table.
            Dim Empty As Boolean = True
            For i As Integer = 0 To TaskSet.Tasks.Length - 1
                If TaskSet.Tasks(i) IsNot Nothing Then
                    Empty = False
                    Exit For
                End If
            Next
            If Empty Then
#If DEBUG Then
                Dim OldCount As Integer = _resourceTaskSets.Count
#End If
                _resourceTaskSets.Remove(key:=Resource)

#If DEBUG Then
                Debug.Assert(_resourceTaskSets.Count = OldCount - 1)
#End If
            End If
        End Sub

        ''' <summary>
        ''' Clears all task list entries for the given resource.
        ''' </summary>
        ''' <param name="Resource">The resource to clear.</param>
        Public Sub ClearResourceTasks(Resource As Resource)
            Dim TaskSet As ResourceTaskSet = Nothing
            If _resourceTaskSets.TryGetValue(Resource, TaskSet) Then

                'Remove all entries for this resource
                For i As Integer = 0 To TaskSet.Tasks.Length - 1
                    Dim Task As TaskListItem = TaskSet.Tasks(i)
                    If Task IsNot Nothing Then
                        If _errorListProvider IsNot Nothing Then
                            _errorListProvider.Tasks.Remove(Task)
                        End If
                    End If
                Next

                '... and then remove the task set itself from the hash table.
#If DEBUG Then
                Dim OldCount As Integer = _resourceTaskSets.Count
#End If
                _resourceTaskSets.Remove(key:=Resource)

#If DEBUG Then
                Debug.Assert(_resourceTaskSets.Count = OldCount - 1)
#End If
            End If
        End Sub

        ''' <summary>
        ''' Adds a resource to the list of resources that need to be checked for errors
        '''   during idle time processing.  When we load a resource file, we only check
        '''   minimally for errors.  We don't do the more expensive check of instantiating
        '''   the resource, we save that for idle time.
        ''' </summary>
        ''' <param name="Resource">The Resource which should be delay-checked later for errors.</param>
        ''' <remarks>
        ''' It's okay to add the same resource multiple times.
        ''' </remarks>
        Public Sub DelayCheckResourceForErrors(Resource As Resource)
            Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Adding resource to list: " & Resource.Name)

            If _resourcesToDelayCheckForErrors.Count = 0 AndAlso Not _delayCheckSuspended Then
                'We need to hook up for idle-time processing so we can delay-check this resource.
                Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Hooking up idle-time processing")
                AddHandler Application.Idle, AddressOf OnDelayCheckForErrors
            End If

            'Add the resource to the list
            If Not _resourcesToDelayCheckForErrors.Contains(Resource) Then
                _resourcesToDelayCheckForErrors.Add(Resource)
            End If
        End Sub

        ''' <summary>
        ''' Causes all the resources in the file to be again queued for validation during
        '''   idle time.
        ''' IMPORTANT NOTE: It is *not* necessary to call this function during resource file
        '''   load, because in that case the resources all gets added to the delay-check
        '''   list during component add.  This is only needed if something has changed that
        '''   might change the validation of some resources.
        ''' </summary>
        Public Sub DelayCheckAllResourcesForErrors()
            StopDelayingCheckingForErrors()
            If _resources IsNot Nothing Then
                For Each resource In _resources.Values
                    DelayCheckResourceForErrors(resource)
                Next
            End If
        End Sub

        ''' <summary>
        ''' Removes a resource from the list of resources that need to be checked for
        '''   errors during idle time processing.  Generally the reason for removing it
        '''   is that it is being deleted and so is no longer valid.
        ''' </summary>
        ''' <param name="Resource">The resource to add.  If it doesn't exist, this call is a NOOP.</param>
        Private Sub RemoveResourceToDelayCheckForErrors(Resource As Resource)
            If _resourcesToDelayCheckForErrors.Contains(Resource) Then
                Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Removing resource from list: " & Resource.Name)
                _resourcesToDelayCheckForErrors.Remove(Resource)

                If _resourcesToDelayCheckForErrors.Count = 0 Then
                    'No more resources to check right now, so we should un-hook our idle-time processing.
                    Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Unhooking idle-time processing")
                    RemoveHandler Application.Idle, AddressOf OnDelayCheckForErrors
                End If
            End If
        End Sub

        'CONSIDER: This event only fires once for every Windows message, which means it may take a while to
        '  get through all the resources and have them checked (processing only occurs *while* the user is
        '  interacting with the shell.
        '  Consider hooking up to the shell's MSO-based idle processing instead, where we can simply keep processing
        '  resources while the user is not interacting with the computer.  See Application.cs.  Might be possible
        '  to search OLE message filter for the current component manager and call FContinueIdle on it.
        '  Or we could continue processing for x milliseconds per call...

        ''' <summary>
        ''' Our idle-time processing which checks resources for errors to be added to the task
        '''   list.  This is used so that we delay loading resources from disk (makes our
        '''   start-up a lot faster).
        ''' </summary>
        ''' <param name="sender">Event sender.</param>
        ''' <param name="e">Event args.</param>
        ''' <remarks>
        ''' Idle-time processing is done on the main thread, so there's no need for synchronization.
        ''' We must keep our idle-time processing short, so we currently only process a single
        '''   resource per call.
        ''' </remarks>
        Private Sub OnDelayCheckForErrors(sender As Object, e As EventArgs)
            If _mainThread IsNot System.Threading.Thread.CurrentThread Then
                Debug.Fail("Idle processing is supposed to occur on the main thread!")
                Exit Sub
            End If

            If _resourcesToDelayCheckForErrors.Count > 0 Then
                Dim Resource As Resource = _resourcesToDelayCheckForErrors(0)
                Debug.WriteLineIf(Switches.RSEDelayCheckErrors.TraceVerbose, "Delay-check errors: Processing: " & Resource.Name)

                'Check the resource for errors
                Resource.CheckForErrors(FastChecksOnly:=False)

                '... and remove it from our list.
                RemoveResourceToDelayCheckForErrors(Resource)
            Else
                Debug.Fail("Why didn't we unhook our idle-time processing if there were no more resources to process?")
                RemoveHandler Application.Idle, AddressOf OnDelayCheckForErrors
            End If
        End Sub

        ''' <summary>
        ''' Stops delay-checking for errors, and removes ourselves from idle-time processing.  The list
        '''   of resources to delay-check for errors will be cleared.
        ''' </summary>
        Private Sub StopDelayingCheckingForErrors()
            While _resourcesToDelayCheckForErrors.Count > 0
                RemoveResourceToDelayCheckForErrors(_resourcesToDelayCheckForErrors(0))
            End While
        End Sub

        ''' <summary>
        '''  if suspendIt = true Suspends delay-checking for errors 
        '''   otherwise, resume the delay-checking process
        ''' </summary>
        ''' <param name="suspendIt"></param>
        ''' <remarks>We also use Idle time to load images. The delay checking should be low priority, and need to be disabled until we finish paining the screen.</remarks>
        Friend Sub SuspendDelayingCheckingForErrors(suspendIt As Boolean)
            If suspendIt <> _delayCheckSuspended Then
                _delayCheckSuspended = suspendIt
                If _resourcesToDelayCheckForErrors.Count > 0 Then
                    If _delayCheckSuspended Then
                        RemoveHandler Application.Idle, AddressOf OnDelayCheckForErrors
                    Else
                        AddHandler Application.Idle, AddressOf OnDelayCheckForErrors
                    End If
                End If
            End If
        End Sub
#End Region

#Region "Resource comparer"
        ''' <summary>
        '''  The AlphabetizedOrderComparer is used to sort resource items in alphabet order.
        ''' </summary>
        Private Class AlphabetizedOrderComparer
            Implements IComparer

            Public Shared ReadOnly Instance As New AlphabetizedOrderComparer

            Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
                Dim r1 As Resource = CType(x, Resource)
                Dim r2 As Resource = CType(y, Resource)

                Return StringComparers.ResourceNames.Compare(r1.Name, r2.Name)
            End Function
        End Class

        ''' <summary>
        '''  The OriginalOrderComparer is used to sort resource items and keep the old item before new ones...
        ''' </summary>
        Private Class OriginalOrderComparer
            Implements IComparer

            Public Shared ReadOnly Instance As New OriginalOrderComparer

            Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
                Dim r1 As Resource = CType(x, Resource)
                Dim r2 As Resource = CType(y, Resource)

                If r1.OrderID = r2.OrderID Then
                    Return StringComparers.ResourceNames.Compare(r1.Name, r2.Name)
                Else
                    Return r1.OrderID - r2.OrderID
                End If
            End Function
        End Class
#End Region

#Region "Miscellaneous"

        ''' <summary>
        ''' Scan all resources items and add necessary reference to the project (if possible)
        ''' </summary>
        Friend Sub AddNecessaryReferenceToProject()
            AddNecessaryReferenceToProject(_resources.Values)
        End Sub

        ''' <summary>
        ''' Scan a list of resources items and add necessary reference to the project (if possible)
        ''' </summary>
        ''' <param name="Resources">A collection of resource items </param>
        ''' <remarks>For performance reasons, we processes a collection of items one time</remarks>
        Private Sub AddNecessaryReferenceToProject(Resources As ICollection(Of Resource))
            Debug.Assert(Resources IsNot Nothing, "Invalid Resources collection")

            Dim TypeResolutionService As ITypeResolutionService = View.GetTypeResolutionService()
            If TypeResolutionService IsNot Nothing Then
                ' TypeResolutionService should be there for all language projects. We should skip this function if it is not there.
                ' It could be the scenario that a resource file is opened directly.

                Dim vsLangProj As VSLangProj.VSProject = Nothing

                Dim typeNameCollection As New Specialized.StringCollection
                Dim assemblyCollection As New Specialized.StringCollection

                For Each Resource In Resources
                    Dim resourceType As Type = Nothing
                    Dim cachedValue As Object = Resource.CachedValue
                    If cachedValue IsNot Nothing Then
                        resourceType = cachedValue.GetType()
                    Else
                        ' If it has been resolved once, skip it
                        Dim typeName As String = Resource.ValueTypeName
                        If Not typeNameCollection.Contains(typeName) Then
                            typeNameCollection.Add(typeName)
                            resourceType = TypeResolutionService.GetType(typeName, False)
                        End If
                    End If

                    ' We should ignore, if we couldn't find type...
                    ' We also skip the mscorlib.dll
                    If resourceType IsNot Nothing AndAlso resourceType.Assembly IsNot GetType(String).Assembly Then
                        Dim assemblyName As String = resourceType.Assembly.GetName().Name

                        ' skip the assembly if we have already processed it
                        If Not assemblyCollection.Contains(assemblyName) Then
                            assemblyCollection.Add(assemblyName)

                            Try
                                If vsLangProj Is Nothing Then
                                    Dim dteProject As EnvDTE.Project = ShellUtil.DTEProjectFromHierarchy(View.GetDesignerLoader().VsHierarchy)
                                    If dteProject IsNot Nothing Then
                                        vsLangProj = TryCast(dteProject.Object, VSLangProj.VSProject)
                                    End If

                                    ' NOTE: we only support project system has VsLangProj supporting.
                                    '  This function is not supported in other project systems, like Venus projects
                                    If vsLangProj Is Nothing Then
                                        Return
                                    End If
                                End If

                                If vsLangProj.References.Find(assemblyName) Is Nothing Then
                                    ' Let the project system to handle the exactly version...
                                    vsLangProj.References.Add(assemblyName)
                                End If
                            Catch ex As CheckoutException
                                ' Ignore CheckoutException
                            Catch ex As Exception When ReportWithoutCrash(ex, "Failed to add reference to assembly contining type", NameOf(ResourceFile))
                                ' We should ignore the error if the project system failed to do so..

                                ' NOTE: we need consider to prompt the user an waring message. But it could be very annoying if we pop up many message boxes in one transaction.
                                '  We should consider a global service to collect all warning messages, and show in one dialog box when the transaction is commited.
                            End Try
                        End If
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Returns true iff this resource file is set up for strongly-typed code generation (i.e., a [resxname].vb file
        '''   is created from it).
        ''' </summary>
        Public Function IsGeneratedToCode() As Boolean

            ' Code gen is not supported currently for resw files
            If RootComponent IsNot Nothing AndAlso
               RootComponent.RootDesigner IsNot Nothing AndAlso
               RootComponent.RootDesigner.IsEditingResWFile() Then
                Return False
            End If

            ' Venus project does not support CustomTool property, but they generate code for all resource files under a special directory...
            If RootComponent IsNot Nothing AndAlso RootComponent.IsGlobalResourceInASP() Then
                Return True
            End If

            'Check the Custom Tool property (if there is one in this project type) to see if it's set
            Dim CustomToolValue As String = Nothing
            If RootComponent IsNot Nothing AndAlso RootComponent.RootDesigner IsNot Nothing Then
                Debug.Assert(RootComponent.RootDesigner.HasView)
                Dim View As ResourceEditorView = RootComponent.RootDesigner.GetView()
                CustomToolValue = View.GetCustomToolCurrentValue()
            End If

            Return CustomToolValue <> ""
        End Function

        ''' <summary>
        ''' Gets the CodeDomProvider for this ResX file, or Nothing if none found.
        ''' </summary>
        Public Function GetCodeDomProvider() As CodeDomProvider
            If RootComponent IsNot Nothing AndAlso RootComponent.IsGlobalResourceInASP() Then
                ' Venus project always use C# CodeDomProvider to generate StrongType code for the resource file.
                Return New CSharp.CSharpCodeProvider()
            End If

            If ServiceProvider IsNot Nothing Then
                Try
                    Dim VsmdCodeDomProvider As IVSMDCodeDomProvider = TryCast(ServiceProvider.GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                    If VsmdCodeDomProvider IsNot Nothing Then
                        Return TryCast(VsmdCodeDomProvider.CodeDomProvider, CodeDomProvider)
                    End If
                Catch ex As System.Runtime.InteropServices.COMException
                    Debug.Assert(ex.ErrorCode = Interop.NativeMethods.E_FAIL OrElse ex.ErrorCode = Interop.NativeMethods.E_NOINTERFACE, "Unexpected COM error getting CodeDomProvider from service")
                    Return Nothing
                End Try
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Post a flush and run custom tool request it request not already posted
        ''' </summary>
        Private Sub DelayFlushAndRunCustomTool()
            If Not _delayFlushAndRunCustomToolQueued Then
                If View IsNot Nothing AndAlso View.IsHandleCreated Then
                    View.BeginInvoke(New MethodInvoker(AddressOf DelayFlushAndRunCustomToolImpl))
                    _delayFlushAndRunCustomToolQueued = True
                End If
            End If
        End Sub

        ''' <summary>
        ''' Flush and run the single file generator 
        ''' </summary>
        Private Sub DelayFlushAndRunCustomToolImpl()
            _delayFlushAndRunCustomToolQueued = False
            If View IsNot Nothing AndAlso View.GetDesignerLoader() IsNot Nothing Then
                Try
                    View.GetDesignerLoader().RunSingleFileGenerator(True)
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(DelayFlushAndRunCustomToolImpl), NameOf(ResourceFile))
                    Try
                        View.DsMsgBox(ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Catch ex2 As Exception When ReportWithoutCrash(ex2, "Unable to show exception message for exception", NameOf(DelayFlushAndRunCustomToolImpl))
                    End Try
                End Try
            End If
        End Sub

        Private Function IsDangerous(resxFilePath As String, allBufferText As String) As Boolean
            If _allowMOTW Then
                Return False
            End If

            Dim zone As Security.SecurityZone = GetSecurityZoneOfFile(resxFilePath, Shell.ServiceProvider.GlobalProvider)
            If zone < Security.SecurityZone.Internet Then
                Return False
            End If

            Dim dangerous As Boolean = False

            Using textReader = New StringReader(allBufferText)
                Using reader = New XmlTextReader(textReader)
                    reader.DtdProcessing = DtdProcessing.Ignore
                    reader.XmlResolver = Nothing
                    Try
                        While reader.Read()
                            If reader.NodeType = XmlNodeType.Element Then
                                ' We only want to parse data nodes,
                                ' the mimetype attribute gives the serializer
                                ' that's requested.
                                If reader.LocalName.Equals("data") Then
                                    If reader("mimetype") <> Nothing Then
                                        dangerous = True
                                    End If
                                ElseIf reader.LocalName.Equals("metadata") Then
                                    If reader("mimetype") <> Nothing Then
                                        dangerous = True
                                    End If
                                End If
                            End If
                        End While
                    Catch
                        ' If we hit an error while parsing assume there's a dangerous type in this file.
                        dangerous = True
                    End Try
                End Using
            End Using
            Return dangerous
        End Function

#End Region

    End Class

End Namespace
