' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' The editor factory for the resource editor.  The job of this class is
    '''   simply to create a new resource editor designer when requested by the
    '''   shell.
    ''' </summary>
    <CLSCompliant(False),
    Guid("ff4d6aca-9352-4a5f-821e-f4d6ebdcab11"),
    Shell.ProvideView(Shell.LogicalView.Designer, "Design"),
    Shell.ProvideEditorExtension(GetType(ResourceEditorFactory), ".resw", &H30),
    Shell.ProvideEditorExtension(GetType(ResourceEditorFactory), ".resx", &H30)>
    Friend NotInheritable Class ResourceEditorFactory
        Inherits DesignerFramework.BaseEditorFactory
        Implements IVsTrackProjectDocumentsEvents2

        ' The editor factory GUID.  This guid must be unique for each editor (and hence editor factory)
        Friend Const ResourceEditor_EditorGuid As String = "ff4d6aca-9352-4a5f-821e-f4d6ebdcab11"

        Private _vsTrackProjectDocumentsEventsCookie As UInteger
        Private _vsTrackProjectDocuments As IVsTrackProjectDocuments2

        ''' <summary>
        ''' Creates and registers a new editor factory.  This is called
        '''   by the DesignerPackage when it gets sited.
        ''' We pass in our designer loader type to the base.
        ''' </summary>
        Public Sub New()
            MyBase.New(GetType(ResourceEditorDesignerLoader))
        End Sub

        ''' <summary>
        ''' Provides the (constant) GUID for the subclassed editor factory.
        ''' </summary>
        ''' <remarks>
        ''' Must overridde the base.  Be sure to use the same GUID on the GUID attribute
        '''    attached to the inheriting class.
        ''' </remarks>
        Protected Overrides ReadOnly Property EditorGuid As Guid
            Get
                Return New Guid(ResourceEditor_EditorGuid)
            End Get
        End Property

        ''' <summary>
        ''' Provides the (constant) GUID for the command UI.
        ''' </summary>
        Protected Overrides ReadOnly Property CommandUIGuid As Guid
            Get
                'This is required for key bindings hook-up to work properly.
                Return Constants.MenuConstants.GUID_RESXEditorCommandUI
            End Get
        End Property

        Protected Overrides Sub OnSited()
            If _vsTrackProjectDocuments Is Nothing AndAlso ServiceProvider IsNot Nothing Then
                _vsTrackProjectDocuments = TryCast(ServiceProvider.GetService(GetType(SVsTrackProjectDocuments)), IVsTrackProjectDocuments2)
                If _vsTrackProjectDocuments IsNot Nothing Then
                    ErrorHandler.ThrowOnFailure(_vsTrackProjectDocuments.AdviseTrackProjectDocumentsEvents(Me, _vsTrackProjectDocumentsEventsCookie))
                    Debug.Assert(_vsTrackProjectDocumentsEventsCookie <> 0)
                End If
            End If
        End Sub 'OnSited

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _vsTrackProjectDocumentsEventsCookie <> 0 Then
                    If _vsTrackProjectDocuments IsNot Nothing Then
                        ErrorHandler.ThrowOnFailure(_vsTrackProjectDocuments.UnadviseTrackProjectDocumentsEvents(_vsTrackProjectDocumentsEventsCookie))
                        _vsTrackProjectDocumentsEventsCookie = 0
                        _vsTrackProjectDocuments = Nothing
                    End If
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub 'Dispose

#Region "IVsRunningDocTableEvents2 Implementation"
        ' The following code is stripped from SettingsGlobalObjectProvider
        ' 
        Private Function OnAfterAddDirectoriesEx(cProjects As Integer, cDirectories As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSADDDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterAddFilesEx(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSADDFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveDirectories(cProjects As Integer, cDirectories As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSREMOVEDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveFiles(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSREMOVEFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRenameDirectories(cProjects As Integer, cDirs As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSRENAMEDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories
            Return NativeMethods.S_OK
        End Function

        ' If the resource file is renamed/moved while it's open in the editor, we need to force a reload so that we pick up
        ' the correct new location for relative linked files
        Private Function OnAfterRenameFiles(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSRENAMEFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles
            ' Validate arguments....
            Debug.Assert(rgpProjects IsNot Nothing AndAlso rgpProjects.Length = cProjects, "null rgpProjects or bad-length array")
            Requires.NotNull(rgpProjects, NameOf(rgpProjects))
            If rgpProjects.Length <> cProjects Then Throw Common.CreateArgumentException(NameOf(rgpProjects))

            Debug.Assert(rgFirstIndices IsNot Nothing AndAlso rgFirstIndices.Length = cProjects, "null rgFirstIndices or bad-length array")
            Requires.NotNull(rgFirstIndices, NameOf(rgFirstIndices))
            If rgFirstIndices.Length <> cProjects Then Throw Common.CreateArgumentException(NameOf(rgFirstIndices))

            Debug.Assert(rgszMkOldNames IsNot Nothing AndAlso rgszMkOldNames.Length = cFiles, "null rgszMkOldNames or bad-length array")
            Requires.NotNull(rgszMkOldNames, NameOf(rgszMkOldNames))
            If rgszMkOldNames.Length <> cFiles Then Throw Common.CreateArgumentException(NameOf(rgszMkOldNames))

            Debug.Assert(rgszMkNewNames IsNot Nothing AndAlso rgszMkNewNames.Length = cFiles, "null rgszMkNewNames or bad-length array")
            Requires.NotNull(rgszMkNewNames, NameOf(rgszMkNewNames))
            If rgszMkNewNames.Length <> cFiles Then Throw Common.CreateArgumentException(NameOf(rgszMkNewNames))

            Debug.Assert(rgFlags IsNot Nothing AndAlso rgFlags.Length = cFiles, "null rgFlags or bad-length array")
            Requires.NotNull(rgFlags, NameOf(rgFlags))
            If rgFlags.Length <> cFiles Then Throw Common.CreateArgumentException(NameOf(rgFlags))

            For i As Integer = 0 To cFiles - 1
                If HasResourceFileExtension(rgszMkNewNames(i)) Then
                    Dim designerEventService As IDesignerEventService = TryCast(ServiceProvider.GetService(GetType(IDesignerEventService)), IDesignerEventService)
                    Debug.Assert(designerEventService IsNot Nothing)
                    If designerEventService IsNot Nothing Then
                        For Each host As IDesignerHost In designerEventService.Designers
                            Dim comp As ResourceEditorRootComponent = TryCast(host.RootComponent, ResourceEditorRootComponent)
                            If comp IsNot Nothing AndAlso (String.Equals(rgszMkNewNames(i), comp.ResourceFileName, StringComparison.Ordinal) OrElse String.Equals(rgszMkOldNames(i), comp.ResourceFileName, StringComparison.Ordinal)) Then
                                Dim loaderService As IDesignerLoaderService = TryCast(host.GetService(GetType(IDesignerLoaderService)), IDesignerLoaderService)
                                If loaderService IsNot Nothing Then
                                    comp.RootDesigner.IsInReloading = True
                                    loaderService.Reload()
                                End If
                            End If
                        Next
                    End If
                End If
            Next
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterSccStatusChanged(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgdwSccStatus() As UInteger) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryAddDirectories(pProject As IVsProject, cDirectories As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYADDDIRECTORYFLAGS, pSummaryResult() As VSQUERYADDDIRECTORYRESULTS, rgResults() As VSQUERYADDDIRECTORYRESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryAddFiles(pProject As IVsProject, cFiles As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYADDFILEFLAGS, pSummaryResult() As VSQUERYADDFILERESULTS, rgResults() As VSQUERYADDFILERESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryAddFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRemoveDirectories(pProject As IVsProject, cDirectories As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYREMOVEDIRECTORYFLAGS, pSummaryResult() As VSQUERYREMOVEDIRECTORYRESULTS, rgResults() As VSQUERYREMOVEDIRECTORYRESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRemoveFiles(pProject As IVsProject, cFiles As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYREMOVEFILEFLAGS, pSummaryResult() As VSQUERYREMOVEFILERESULTS, rgResults() As VSQUERYREMOVEFILERESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRenameDirectories(pProject As IVsProject, cDirs As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSQUERYRENAMEDIRECTORYFLAGS, pSummaryResult() As VSQUERYRENAMEDIRECTORYRESULTS, rgResults() As VSQUERYRENAMEDIRECTORYRESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRenameFiles(pProject As IVsProject, cFiles As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSQUERYRENAMEFILEFLAGS, pSummaryResult() As VSQUERYRENAMEFILERESULTS, rgResults() As VSQUERYRENAMEFILERESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles
            Return NativeMethods.S_OK
        End Function

#End Region

        ''' <summary>
        ''' This method is called by the Environment (inside IVsUIShellOpenDocument::
        ''' OpenStandardEditor and OpenSpecificEditor) to map a LOGICAL view to a 
        ''' PHYSICAL view. A LOGICAL view identifies the purpose of the view that is
        ''' desired (e.g. a view appropriate for Debugging [LOGVIEWID_Debugging], or a 
        ''' view appropriate for text view manipulation as by navigating to a find
        ''' result [LOGVIEWID_TextView]). A PHYSICAL view identifies an actual type 
        ''' of view implementation that an IVsEditorFactory can create. 
        ''' 	
        ''' NOTE: Physical views are identified by a string of your choice with the 
        ''' one constraint that the default/primary physical view for an editor  
        ''' *MUST* use a NULL string as its physical view name (*pbstrPhysicalView = NULL).
        ''' 	
        ''' NOTE: It is essential that the implementation of MapLogicalView properly
        ''' validates that the LogicalView desired is actually supported by the editor.
        ''' If an unsupported LogicalView is requested then E_NOTIMPL must be returned.
        ''' 	
        ''' NOTE: The special Logical Views supported by an Editor Factory must also 
        ''' be registered in the local registry hive. LOGVIEWID_Primary is implicitly 
        ''' supported by all editor types and does not need to be registered.
        ''' For example, an editor that supports a ViewCode/ViewDesigner scenario
        ''' might register something like the following:
        ''' HKLM\Software\Microsoft\VisualStudio\[CurrentVSVersion]\Editors\
        ''' {...guidEditor...}\
        ''' LogicalViews\
        ''' {...LOGVIEWID_TextView...} = s ''
        ''' {...LOGVIEWID_Code...} = s ''
        ''' {...LOGVIEWID_Debugging...} = s ''
        ''' {...LOGVIEWID_Designer...} = s 'Form'
        ''' </summary>
        Protected Overrides Function MapLogicalView(ByRef LogicalView As Guid, ByRef PhysicalViewOut As String) As Integer

            'The default view must have the value of Nothing.
            PhysicalViewOut = Nothing

            If LogicalView.Equals(LOGVIEWID.LOGVIEWID_Primary) OrElse LogicalView.Equals(LOGVIEWID.LOGVIEWID_Designer) Then
                ' if it's primary or designer, then that's our bread & butter, so return S_OK
                '
                Return NativeMethods.S_OK
            Else
                ' anything else should return E_NOTIMPL
                '
                Return NativeMethods.E_NOTIMPL
            End If
        End Function

    End Class
End Namespace
