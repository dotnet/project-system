' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    '**************************************************************************
    ';ApplicationDesignerEditorFactory
    '
    'Remarks:
    '   The editor factory for the application designer. The job of this class is
    '   simply to create a new application designer when requested by the
    '   shell.
    '**************************************************************************
    <CLSCompliant(False),
    Guid(ApplicationDesignerEditorFactory.EditorGuidString),
    ProvideView(LogicalView.Designer, "Design")>
    Public NotInheritable Class ApplicationDesignerEditorFactory
        Implements IVsEditorFactory

        Friend Const EditorGuidString = "04b8ab82-a572-4fef-95ce-5222444b6b64"

        'The all important GUIDs 
        Private Shared ReadOnly s_editorGuid As New Guid(EditorGuidString)
        Private Shared ReadOnly s_commandUIGuid As New Guid("{d06cd5e3-d961-44dc-9d80-c89a1a8d9d56}")

        'GUID of the new project properties editor, to delegate to if enabled
        Private Shared ReadOnly s_newEditorGuid As New Guid("{990036EB-F67A-4B8A-93D4-4663DB2A1033}")

        'Exposing the GUID for the rest of the assembly to see
        Public Shared ReadOnly Property EditorGuid As Guid
            Get
                Return s_editorGuid
            End Get
        End Property

        'Exposing the GUID for the rest of the assembly to see
        Public Shared ReadOnly Property CommandUIGuid As Guid
            Get
                Return s_commandUIGuid
            End Get
        End Property

        Private _site As OLE.Interop.IServiceProvider 'The site that owns this editor factory
        Private _siteProvider As ServiceProvider 'The service provider from m_Site

        ''' <summary>
        ''' Creates a new editor for the given pile of flags.  Helper function for the overload
        ''' which implements IVsEditorFactory.CreateEditorInstance
        ''' </summary>
        ''' <param name="FileName">[In] Filename being opened</param>
        ''' <param name="Hierarchy">[In] IVsHierarchy of node being opened</param>
        ''' <param name="ItemId">[In] ItemId for node being opened</param>
        ''' <param name="ExistingDocData">[In] Existing DocData if any</param>
        ''' <param name="DocView">Returns the IVsWindowPane object</param>
        ''' <param name="DocData">Returns DocData object</param>
        ''' <param name="Caption">Returns caption for document window</param>
        ''' <param name="CmdUIGuid">Returns guid for CMDUI</param>
        ''' <param name="pgrfCDW">[out] Flags to be passed to CreateDocumentWindow</param>
        Private Function InternalCreateEditorInstance(FileName As String,
                Hierarchy As IVsHierarchy,
                ItemId As UInteger,
                ExistingDocData As Object,
                ByRef DocView As Object,
                ByRef DocData As Object,
                ByRef Caption As String,
                ByRef CmdUIGuid As Guid,
                ByRef pgrfCDW As Integer) As Integer
            pgrfCDW = 0
            CmdUIGuid = Guid.Empty

            Dim DesignerLoader As ApplicationDesignerLoader = Nothing

            Try
                Using New Common.WaitCursor

                    DocView = Nothing
                    DocData = Nothing
                    Caption = Nothing

                    Dim DesignerService As IVSMDDesignerService = CType(_siteProvider.GetService(GetType(IVSMDDesignerService)), IVSMDDesignerService)
                    If DesignerService Is Nothing Then
                        Throw New Exception(My.Resources.Designer.GetString(My.Resources.Designer.DFX_EditorNoDesignerService, FileName))
                    End If

                    If ExistingDocData Is Nothing Then
                        'We do not support being loaded without a DocData on the project file being passed to us by
                        '  QI'ing for  IVsHierarchy.
                        Trace.WriteLine("*** ApplicationDesignerEditorFactory: ExistingDocData = Nothing, returning VS_E_UNSUPPORTEDFORMAT - we shouldn't be called this way")
                        Return VSErrorCodes.VS_E_UNSUPPORTEDFORMAT
                    Else
                        'Verify that the DocData passed in to us really is the project file
                        Dim VsHierarchy As IVsHierarchy = TryCast(ExistingDocData, IVsHierarchy)
                        If VsHierarchy Is Nothing Then
                            Debug.Fail("The DocData passed in to the project designer was not the project file - this is not supported.")
                            Return VSErrorCodes.VS_E_UNSUPPORTEDFORMAT
                        End If

                        DocData = ExistingDocData
                    End If

                    DesignerLoader = CType(DesignerService.CreateDesignerLoader(GetType(ApplicationDesignerLoader).AssemblyQualifiedName), ApplicationDesignerLoader)
                    DesignerLoader.InitializeEx(_siteProvider, Hierarchy, ItemId, DocData)
                    'If ExistingDocData IsNot Nothing Then
                    'Don't pass this value back
                    'DocData = Nothing
                    'End If

                    'Site the TextStream
                    'If TypeOf DocData Is IObjectWithSite Then
                    '   CType(DocData, IObjectWithSite).SetSite(m_Site)
                    'Else
                    '   Debug.Fail("DocData does not implement IObjectWithSite")
                    'End If

                    Dim OleProvider As OLE.Interop.IServiceProvider = CType(_siteProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Dim Designer As IVSMDDesigner = DesignerService.CreateDesigner(OleProvider, DesignerLoader)

                    Debug.Assert(Designer IsNot Nothing, "Designer service should have thrown if it had a problem.")

                    'Set the out params
                    DocView = Designer.View 'Gets the object that can support IVsWindowPane

                    'An empty caption allows the projectname to be used as the caption
                    'The OpenSpecificEditor call takes a "%1" for the user caption.  We currently use the 
                    ' project name
                    Caption = ""

                    'Set the command UI
                    CmdUIGuid = s_commandUIGuid

                    'Flags to pass back, these flags get passed toCreateDocumentWindow.  We need these because of the
                    '  way the project designer is shown by the project system.
                    pgrfCDW = _VSRDTFLAGS.RDT_VirtualDocument Or _VSRDTFLAGS.RDT_ProjSlnDocument
                End Using

            Catch ex As Exception

                If DesignerLoader IsNot Nothing Then
                    'We need to let the DesignerLoader disconnect from events
                    DesignerLoader.Dispose()
                End If

                Throw New Exception(My.Resources.Designer.GetString(My.Resources.Designer.DFX_CreateEditorInstanceFailed_Ex, ex.Message))
            End Try
        End Function

        ''' <summary>
        ''' Disconnect from the owning site
        ''' </summary>
        Public Function Close() As Integer Implements IVsEditorFactory.Close
            _siteProvider = Nothing
            _site = Nothing
        End Function

        ''' <summary>
        ''' Wrapper of COM interface which delegates to Internal
        ''' </summary>
        Private Function IVsEditorFactory_CreateEditorInstance(
                vscreateeditorflags As UInteger,
                FileName As String,
                PhysicalView As String,
                Hierarchy As IVsHierarchy,
                Itemid As UInteger,
                ExistingDocDataPtr As IntPtr,
                ByRef DocViewPtr As IntPtr,
                ByRef DocDataPtr As IntPtr,
                ByRef Caption As String,
                ByRef CmdUIGuid As Guid,
                ByRef pgrfCDW As Integer) As Integer _
        Implements IVsEditorFactory.CreateEditorInstance

            ' If we're using the new project properties editor, delegate to its editor factory
            Dim shouldUseNewEditor As Boolean = Hierarchy.IsCapabilityMatch("ProjectPropertiesEditor")

            Common.TelemetryLogger.LogEditorCreation(shouldUseNewEditor, FileName, PhysicalView)

            If shouldUseNewEditor Then
                Return GetNewEditorFactory().CreateEditorInstance(
                    vscreateeditorflags,
                    FileName,
                    PhysicalView,
                    Hierarchy,
                    ItemId,
                    ExistingDocDataPtr,
                    DocViewPtr,
                    DocDataPtr,
                    Caption,
                    CmdUIGuid,
                    pgrfCDW)
            End If

            Dim ExistingDocData As Object = Nothing
            Dim DocView As Object = Nothing
            Dim DocData As Object = Nothing

            DocViewPtr = IntPtr.Zero
            DocDataPtr = IntPtr.Zero

            If Not ExistingDocDataPtr.Equals(IntPtr.Zero) Then
                ExistingDocData = Marshal.GetObjectForIUnknown(ExistingDocDataPtr)
            End If

            Caption = Nothing

            Dim hr As Integer = InternalCreateEditorInstance(FileName, Hierarchy, Itemid, ExistingDocData,
                DocView, DocData, Caption, CmdUIGuid, pgrfCDW)

            If NativeMethods.Failed(hr) Then
                Return hr
            End If

            If DocView IsNot Nothing Then
                DocViewPtr = Marshal.GetIUnknownForObject(DocView)
            End If

            If DocData IsNot Nothing Then
                DocDataPtr = Marshal.GetIUnknownForObject(DocData)
            End If

            Return hr
        End Function

        Private Function GetNewEditorFactory() As IVsEditorFactory
            Dim vsUIShellOpenDocument = TryCast(_siteProvider.GetService(GetType(IVsUIShellOpenDocument)), IVsUIShellOpenDocument)

            Assumes.Present(vsUIShellOpenDocument)

            Dim newEditorGuid = s_newEditorGuid
            Dim newEditorFactory as IVsEditorFactory = Nothing

            Verify.HResult(vsUIShellOpenDocument.GetStandardEditorFactory(
                dwReserved := 0,
                newEditorGuid,
                pszMkDocument := Nothing,
                VSConstants.LOGVIEWID.Designer_guid,
                pbstrPhysicalView := Nothing,
                newEditorFactory))

            Assumes.Present(newEditorFactory)
            Return newEditorFactory
        End Function

        ''' <summary>
        ''' We only support the default view
        ''' </summary>
        ''' <param name="rguidLogicalView"></param>
        ''' <param name="pbstrPhysicalView"></param>
        Public Function MapLogicalView(ByRef rguidLogicalView As Guid, ByRef pbstrPhysicalView As String) As Integer Implements IVsEditorFactory.MapLogicalView

            ' NOTE we do not have a hierarchy, so don't know if this is a CPS project. We don't know whether to delegate to
            ' Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.Designer.ProjectPropertiesEditorFactory.
            ' Therefore we ensure the logic in both implementations of MapLogicalView is identical.

            pbstrPhysicalView = Nothing

            ' The designer nominally supports VSConstants.LOGVIEWID.Designer_guid however it is also called with other GUIDs
            ' that are for the various sub-tabs of the property pages
            ' Rather than hard code those here, we simply allow through everything except TextView, as that is
            ' used when opening files for text editing, and we want the project file to be editable as XML in that scenario

            If rguidLogicalView = VSConstants.LOGVIEWID.TextView_guid Then
                Return NativeMethods.E_NOTIMPL
            Else
                Return NativeMethods.S_OK
            End If
        End Function

        ''' <summary>
        ''' Called by owning site after creation
        ''' </summary>
        ''' <param name="Site"></param>
        Public Function SetSite(Site As OLE.Interop.IServiceProvider) As Integer Implements IVsEditorFactory.SetSite
            'This same Site already set?  Or Site not yet initialized (= Nothing)?  If so, NOP.
            If _site Is Site Then
                Exit Function
            End If
            'Site is different - set it
            _site = Site
            _siteProvider = New ServiceProvider(Site)
        End Function

    End Class

End Namespace
