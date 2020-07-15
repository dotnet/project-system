' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices

Imports EnvDTE

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensibilitySolutionService
    ''' <summary>
    ''' Main entry to My Extensibility service.
    ''' This class manages the My Extensibility services for each VB project in a Solution.
    ''' One instance of this class per VS instance (solution).
    ''' </summary>
    ''' <remarks>
    ''' [OBSOLETE] (Keep for reference only)
    ''' - Once the manager is created, it will listen to EnvDTE.Events2.SolutionEvents
    '''   to update / clear the project services and to handle Zero-Impact-Project (ZIP).
    ''' - Special handling for ZIP: When a Save All occurs to DIFFERENT disk drive than the temporary ZIP,
    '''   (the project system will call the compiler to Remove,Add,Remove,Add each new references).
    '''   VB compiler does not know much about this event so each project service will listen to
    '''   _dispReferencesEvents themselves. MyExtensibilitySolutionService will not inform 
    '''   project service of reference changes once a project service exists.
    ''' [/OBSOLETE]
    ''' The solution above leads to DevDiv Bugs 51380. If multiple assemblies are being added,
    ''' the ProjectService will only know about the first assemblies.
    ''' The issue with ZIP does not exist with later Orcas build.
    ''' </remarks>
    Friend Class MyExtensibilitySolutionService

#Region "Shared methods"
        ''' ;Instance
        ''' <summary>
        ''' Shared property to obtain the instance of MyExtensibilityManager associated with
        ''' the current VS environment.
        ''' </summary>
        Public Shared ReadOnly Property Instance As MyExtensibilitySolutionService
            Get
                If s_sharedInstance Is Nothing Then
                    s_sharedInstance = New MyExtensibilitySolutionService(VBPackage.Instance)
                End If

                Debug.Assert(s_sharedInstance IsNot Nothing)
                Return s_sharedInstance
            End Get
        End Property

        ''' ;IdeStatusBar
        ''' <summary>
        ''' Shared property to obtain the current VS status bar.
        ''' </summary>
        Public Shared ReadOnly Property IdeStatusBar As VsStatusBarWrapper
            Get
                If s_ideStatusBar Is Nothing Then
                    Dim vsStatusBar As IVsStatusbar = TryCast(
                        VBPackage.Instance.GetService(GetType(IVsStatusbar)), IVsStatusbar)
                    If vsStatusBar IsNot Nothing Then
                        s_ideStatusBar = New VsStatusBarWrapper(vsStatusBar)
                    End If

                    Debug.Assert(s_ideStatusBar IsNot Nothing, "Could not get IVsStatusBar!")
                End If

                Return s_ideStatusBar
            End Get
        End Property

        Private Shared s_sharedInstance As MyExtensibilitySolutionService ' shared instance for the current VS environment.
        Private Shared s_ideStatusBar As VsStatusBarWrapper ' shared instance of the current VS status bar.
#End Region

        ''' ;GetService
        ''' <summary>
        ''' Obtain the specified service.
        ''' </summary>
        Public Function GetService(serviceType As Type) As Object
            Return _vbPackage.GetService(serviceType)
        End Function

        ''' ;ReferenceAdded
        ''' <summary>
        ''' Notify the project's My Extensibility service that a reference has been added.
        ''' </summary>
        ''' <remarks>VB Compiler will call this method through VBReferenceChangedService.</remarks>
        Public Sub ReferenceAdded(projectHierarchy As IVsHierarchy, assemblyInfo As String)
            HandleReferenceChange(projectHierarchy, assemblyInfo, AddRemoveAction.Add)
        End Sub

        ''' ;ReferenceRemoved
        ''' <summary>
        ''' Notify the project's My Extensibility service that a reference has been removed.
        ''' </summary>
        ''' <remarks>VB Compiler will call this method through VBReferenceChangedService.</remarks>
        Public Sub ReferenceRemoved(projectHierarchy As IVsHierarchy, assemblyInfo As String)
            HandleReferenceChange(projectHierarchy, assemblyInfo, AddRemoveAction.Remove)
        End Sub

        ''' ;GetProjectService
        ''' <summary>
        ''' Get the MyExtensibilityProjectService for the given IVsHierarchy.
        ''' </summary>
        ''' <remarks>This can be invoked by VB Compiler (through ReferenceAdded, ReferenceRemoved) or
        ''' My Extensibility Property Page.</remarks>
        Public Function GetProjectService(projectHierarchy As IVsHierarchy) _
                As MyExtensibilityProjectService

            ' Expect an IVsHierarchy but if none is provided, attempt to get it from IVsMonitorSelection.
            If projectHierarchy Is Nothing Then
                Try
                    Dim vsMonitorSelection As IVsMonitorSelection = TryCast(
                        _vbPackage.GetService(GetType(IVsMonitorSelection)), IVsMonitorSelection)

                    If vsMonitorSelection IsNot Nothing Then
                        Dim vsHierarchyPointer As IntPtr = IntPtr.Zero
                        Dim itemID As UInteger = VSITEMID.NIL
                        Dim vsMultiItemSelect As IVsMultiItemSelect = Nothing
                        Dim selectionContainerPointer As IntPtr = IntPtr.Zero

                        Try
                            vsMonitorSelection.GetCurrentSelection(
                                vsHierarchyPointer, itemID, vsMultiItemSelect, selectionContainerPointer)
                        Finally
                            If selectionContainerPointer <> IntPtr.Zero Then
                                Marshal.Release(selectionContainerPointer)
                            End If

                            If vsHierarchyPointer <> IntPtr.Zero Then
                                projectHierarchy = TryCast(
                                    Marshal.GetObjectForIUnknown(vsHierarchyPointer), IVsHierarchy)
                                Marshal.Release(vsHierarchyPointer)
                                vsHierarchyPointer = IntPtr.Zero
                            End If
                        End Try
                    End If ' If vsMonitorSelection IsNot Nothing
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(GetProjectService), NameOf(MyExtensibilitySettings))
                    ' Ignore exceptions.
                End Try
            End If

            ' Get the EnvDTE.Project from the project hierarchy.
            Dim project As Project = Nothing
            If projectHierarchy IsNot Nothing Then
                Dim projectObject As Object = Nothing
                Dim hr As Integer = projectHierarchy.GetProperty(
                    VSITEMID.ROOT, CInt(__VSHPROPID.VSHPROPID_ExtObject), projectObject)

                If VSErrorHandler.Succeeded(hr) AndAlso projectObject IsNot Nothing Then
                    project = TryCast(projectObject, Project)
                End If
            End If

            ' Create a MyExtensibilityProjectService for the current project if need to
            If project IsNot Nothing Then
                If Not _projectServices.ContainsKey(project) Then
                    _projectServices.Add(project,
                        MyExtensibilityProjectService.CreateNew(_vbPackage, project, projectHierarchy, ExtensibilitySettings))
                End If
                Return _projectServices(project)
            End If

            Return Nothing

        End Function

        Public ReadOnly Property TrackProjectDocumentsEvents As TrackProjectDocumentsEventsHelper
            Get
                If _trackProjectDocumentsEvents Is Nothing Then
                    _trackProjectDocumentsEvents = TrackProjectDocumentsEventsHelper.GetInstance(_vbPackage)
                End If
                Return _trackProjectDocumentsEvents
            End Get
        End Property

        ''' ;New
        ''' <summary>
        ''' Private constructor since MyExtensibilityManager can be accessed through
        ''' shared property Instance.
        ''' </summary>
        Private Sub New(vbPackage As VBPackage)
            Debug.Assert(vbPackage IsNot Nothing, "vbPackage Is Nothing")
            _vbPackage = vbPackage
            AddEnvDTEEvents()
        End Sub

        ''' ;ExtensibilitySettings
        ''' <summary>
        ''' Lazy-initialized My Extensibility settings containing information about extension templates.
        ''' </summary>
        Private ReadOnly Property ExtensibilitySettings As MyExtensibilitySettings
            Get
                If _extensibilitySettings Is Nothing Then

                    Dim vsAppDataDir As String = String.Empty
                    Dim vsShell As IVsShell = TryCast(_vbPackage.GetService(GetType(IVsShell)), IVsShell)
                    If vsShell IsNot Nothing Then
                        Dim appDataDir As Object = Nothing
                        Dim hr As Integer = vsShell.GetProperty(__VSSPROPID.VSSPROPID_AppDataDir, appDataDir)
                        If VSErrorHandler.Succeeded(hr) Then
                            vsAppDataDir = CStr(appDataDir)
                        End If
                    End If
                    _extensibilitySettings = New MyExtensibilitySettings(vsAppDataDir)
                End If
                Return _extensibilitySettings
            End Get
        End Property

        ''' ;HandleReferenceChange
        ''' <summary>
        ''' If needed, notify the given project's My Extensibility service that a reference has been added or removed.
        ''' </summary>
        ''' <remarks>
        ''' The compiler will initialize a My Extensibility project service when a reference is added or removed.
        ''' After that, the project service will listen to reference added or removed event itself (to avoid ZIP problem).
        ''' Therefore, if a project service already exists, do not notify it.
        ''' </remarks>
        Private Sub HandleReferenceChange(projectHierarchy As IVsHierarchy, assemblyInfo As String,
                action As AddRemoveAction)
            ' assemblyInfo can be NULL in case of unmanaged assembly.
            If StringIsNullEmptyOrBlank(assemblyInfo) Then
                Exit Sub
            End If

            Switches.TraceMyExtensibility(TraceLevel.Verbose, String.Format("MyExtensibilitySolutionService.HandleReferenceChange: Entry. assemblyInfo='{0}'.", assemblyInfo))

            Dim projectService As MyExtensibilityProjectService = GetProjectService(projectHierarchy)
            If projectService IsNot Nothing Then
                Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.HandleReferenceChange: ProjectService exists, notifying.")
                If action = AddRemoveAction.Add Then
                    projectService.ReferenceAdded(assemblyInfo)
                Else
                    projectService.ReferenceRemoved(assemblyInfo)
                End If
            End If

            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.HandleReferenceChange: Exit.")
        End Sub

#Region "SolutionEvents and DTEEvents"

        ''' ;AddEnvDTEEvents
        ''' <summary>
        ''' Hook ourselves up to listen to DTE and solution events.
        ''' </summary>
        Private Sub AddEnvDTEEvents()
            Dim dte As EnvDTE80.DTE2 = TryCast(_vbPackage.GetService(GetType(_DTE)), EnvDTE80.DTE2)
            If dte IsNot Nothing Then
                Dim events As EnvDTE80.Events2 = TryCast(dte.Events, EnvDTE80.Events2)
                If events IsNot Nothing Then
                    _solutionEvents = events.SolutionEvents
                    If _solutionEvents IsNot Nothing Then
                        AddHandler _solutionEvents.AfterClosing,
                            New _dispSolutionEvents_AfterClosingEventHandler(
                            AddressOf SolutionEvents_AfterClosing)
                        AddHandler _solutionEvents.ProjectRemoved,
                            New _dispSolutionEvents_ProjectRemovedEventHandler(
                            AddressOf SolutionEvents_ProjectRemoved)
                    End If
                    _dteEvents = events.DTEEvents
                    If _dteEvents IsNot Nothing Then
                        AddHandler _dteEvents.OnBeginShutdown,
                            New _dispDTEEvents_OnBeginShutdownEventHandler(
                            AddressOf DTEEvents_OnBeginShutDown)
                    End If
                End If
            End If
        End Sub

        ''' ;RemoveSolutionEvents
        ''' <summary>
        ''' Remove ourselves as listener of DTE and solution events.
        ''' </summary>
        Private Sub RemoveEnvDTEEvents()
            If _solutionEvents IsNot Nothing Then
                RemoveHandler _solutionEvents.AfterClosing,
                    New _dispSolutionEvents_AfterClosingEventHandler(
                    AddressOf SolutionEvents_AfterClosing)
                RemoveHandler _solutionEvents.ProjectRemoved,
                    New _dispSolutionEvents_ProjectRemovedEventHandler(
                    AddressOf SolutionEvents_ProjectRemoved)
                _solutionEvents = Nothing
            End If
            If _dteEvents IsNot Nothing Then
                RemoveHandler _dteEvents.OnBeginShutdown,
                    New _dispDTEEvents_OnBeginShutdownEventHandler(
                    AddressOf DTEEvents_OnBeginShutDown)
                _dteEvents = Nothing
            End If
        End Sub

        ''' ;SolutionEvents_AfterClosing
        ''' <summary>
        ''' Handle solution's AfterClosing events and clear our collection of project services.
        ''' </summary>
        Private Sub SolutionEvents_AfterClosing()
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: Entry. Clear project services dictionary.")
            _projectServices.Clear()

            If _trackProjectDocumentsEvents IsNot Nothing Then
                Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: UnAdviseTrackProjectDocumentsEvents.")
                _trackProjectDocumentsEvents.UnAdviseTrackProjectDocumentsEvents()
                Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: Clear m_TrackProjectDocumentsEvents.")
                _trackProjectDocumentsEvents = Nothing
            End If
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.SolutionEvents_AfterClosing: Exit.")
        End Sub

        ''' ;SolutionEvents_ProjectRemoved
        ''' <summary>
        ''' Handle ProjectRemoved event and remove the associate project service from our collection.
        ''' </summary>
        Private Sub SolutionEvents_ProjectRemoved(project As Project)
            If project Is Nothing Then
                Exit Sub
            End If

            If _projectServices.ContainsKey(project) Then
                Dim removedProjectService As MyExtensibilityProjectService = _projectServices(project)
                _projectServices.Remove(project)
                If removedProjectService IsNot Nothing Then
                    removedProjectService.Dispose()
                End If
            End If
        End Sub

        ''' ;DTEEvents_OnBeginShutDown
        ''' <summary>
        ''' Handle DTE OnBeginShutDown event to remove our event handlers.
        ''' </summary>
        Private Sub DTEEvents_OnBeginShutDown()
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.DTEEvents_OnBeginShutDown: Entry. Call AfterClosing.")
            SolutionEvents_AfterClosing() ' Dispose all project services.
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.DTEEvents_OnBeginShutDown: RemoveEnvDTEEvents.")
            RemoveEnvDTEEvents()
            Switches.TraceMyExtensibility(TraceLevel.Verbose, "MyExtensibilitySolutionService.DTEEvents_OnBeginShutDown: Exit.")
        End Sub
#End Region

        Private ReadOnly _vbPackage As VBPackage
        ' Collection of MyExtensibilityProjectServices for each known project.
        Private ReadOnly _projectServices As New Dictionary(Of Project, MyExtensibilityProjectService)()
        ' My Extensibility settings of the current VS. Lazy init.
        Private _extensibilitySettings As MyExtensibilitySettings
        ' Handle solution closing and project removal events.
        Private _solutionEvents As SolutionEvents
        ' Handle DTE closing events
        Private _dteEvents As DTEEvents
        ' lazy-init instance of TrackProjectDocumentsEventsHelper
        Private _trackProjectDocumentsEvents As TrackProjectDocumentsEventsHelper

    End Class

End Namespace
