' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    '**************************************************************************
    ';PropPageDesignerEditorFactory
    '
    'Remarks:
    '   The editor factory for the resource editor.  The job of this class is
    '   simply to create a new resource editor designer when requested by the
    '   shell.
    '**************************************************************************
    <CLSCompliant(False), _
    Guid("b270807c-d8c6-49eb-8ebe-8e8d566637a1")> _
    Public NotInheritable Class PropPageDesignerEditorFactory
        Implements IVsEditorFactory

        'The all important GUIDs 
        '    Private Shared ReadOnly s_editorGuid As New Guid("{b270807c-d8c6-49eb-8ebe-8e8d566637a1}")
        '  Private Shared ReadOnly s_commandUIGuid As New Guid("{86670efa-3c28-4115-8776-a4d5bb1f27cc}")

        'Exposing the GUID for the rest of the assembly to see
        Public Shared ReadOnly Property EditorGuid() As New Guid("{b270807c-d8c6-49eb-8ebe-8e8d566637a1}")

        'Exposing the GUID for the rest of the assembly to see
        Public Shared ReadOnly Property CommandUIGuid() As New Guid("{86670efa-3c28-4115-8776-a4d5bb1f27cc}")


        Private _site As Object 'The site that owns this editor factory
        Private _siteProvider As Shell.ServiceProvider 'The service provider from m_Site

        ''' <summary>
        ''' Creates a new editor for the given pile of flags.  Helper function for the overload
        ''' which implements IVsEditorFactory.CreateEditorInstance
        ''' </summary>
        ''' <param name="VsCreateEditorFlags"></param>
        ''' <param name="FileName">[In] Filename being opened</param>
        ''' <param name="PhysicalView"></param>
        ''' <param name="Hierarchy">[In] IVsHierarchy of node being opened</param>
        ''' <param name="ItemId">[In] ItemId for node being opened</param>
        ''' <param name="ExistingDocData">[In] Existing DocData if any</param>
        ''' <param name="DocView">Returns the IVsWindowPane object</param>
        ''' <param name="DocData">Returns DocData object</param>
        ''' <param name="Caption">Returns caption for document window</param>
        ''' <param name="CmdUIGuid">Returns guid for CMDUI</param>
        ''' <param name="Canceled">Returns True if user canceled</param>
        ''' <remarks></remarks>
        Private Sub InternalCreateEditorInstance(
                                                 VsCreateEditorFlags As UInt32,
                                                 FileName As String,
                                                 PhysicalView As String,
                                                 Hierarchy As IVsHierarchy,
                                                 ItemId As UInt32,
                                                 ExistingDocData As Object,
                                           ByRef DocView As Object,
                                           ByRef DocData As Object,
                                           ByRef Caption As String,
                                           ByRef CmdUIGuid As Guid,
                                           ByRef Canceled As Boolean)
            Canceled = False
            CmdUIGuid = Guid.Empty

            Dim DesignerLoader As PropPageDesignerLoader = Nothing

            Try
                Using New Common.WaitCursor

                    DocView = Nothing
                    DocData = Nothing
                    Caption = Nothing

                    Dim DesignerService As IVSMDDesignerService = CType(_siteProvider.GetService(GetType(IVSMDDesignerService)), IVSMDDesignerService)
                    If DesignerService Is Nothing Then
                        Throw New Exception(SR.GetString(SR.DFX_EditorNoDesignerService, FileName))
                    End If

                    If ExistingDocData Is Nothing Then
                        DocData = New PropPageDesignerDocData(_siteProvider)
                    Else
                        Throw New COMException(SR.GetString(SR.DFX_IncompatibleBuffer), AppDesInterop.NativeMethods.VS_E_INCOMPATIBLEDOCDATA)
                    End If

                    DesignerLoader = CType(DesignerService.CreateDesignerLoader(GetType(PropPageDesignerLoader).AssemblyQualifiedName), PropPageDesignerLoader)
                    DesignerLoader.InitializeEx(_siteProvider, Hierarchy, ItemId, DocData)

                    Dim OleProvider As OLE.Interop.IServiceProvider = CType(_siteProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Dim Designer As IVSMDDesigner = DesignerService.CreateDesigner(OleProvider, DesignerLoader)

                    'Site the TextStream
                    If TypeOf DocData Is IObjectWithSite Then
                        CType(DocData, IObjectWithSite).SetSite(_site)
                    Else
                        Debug.Fail($"{NameOf(DocData)} does not implement {NameOf(IObjectWithSite)}")
                    End If

                    Debug.Assert(Not (Designer Is Nothing), $"{NameOf(Designer)} service should have thrown if it had a problem.")

                    'Set the out params
                    DocView = Designer.View 'Gets the object that can support IVsWindowPane

                    Caption = "" ' Leave empty - The property page Title will appear as the caption 'Application|References|Debug etc.'

                    'Set the command UI
                    CmdUIGuid = CommandUIGuid
                End Using

            Catch ex As Exception

                If DesignerLoader IsNot Nothing Then
                    'We need to let the DesignerLoader disconnect from events
                    DesignerLoader.Dispose()
                End If

                Throw New Exception(SR.GetString(SR.DFX_CreateEditorInstanceFailed_Ex, ex.Message))
            End Try
        End Sub


        ''' <summary>
        ''' Disconnect from the owning site
        ''' </summary>
        Public Function Close() As Integer Implements Shell.Interop.IVsEditorFactory.Close
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
                                                          ByRef FCanceled As Integer
                                                               ) As Integer Implements IVsEditorFactory.CreateEditorInstance

            Dim ExistingDocData As Object = Nothing
            Dim DocView As Object = Nothing
            Dim DocData As Object = Nothing
            Dim CanceledAsBoolean As Boolean = False

            DocViewPtr = IntPtr.Zero
            DocDataPtr = IntPtr.Zero

            If Not ExistingDocDataPtr.Equals(IntPtr.Zero) Then
                ExistingDocData = Marshal.GetObjectForIUnknown(ExistingDocDataPtr)
            End If

            Caption = Nothing

            InternalCreateEditorInstance(
                                          vscreateeditorflags, FileName, PhysicalView, Hierarchy, Itemid, ExistingDocData,
                                          DocView, DocData, Caption, CmdUIGuid, CanceledAsBoolean
                                         )

            If CanceledAsBoolean Then
                FCanceled = 1
            Else
                FCanceled = 0
            End If

            If (DocView IsNot Nothing) Then DocViewPtr = Marshal.GetIUnknownForObject(DocView)
            If (DocData IsNot Nothing) Then DocDataPtr = Marshal.GetIUnknownForObject(DocData)

        End Function

        ''' <summary>
        ''' We only support the default view
        ''' </summary>
        ''' <param name="rguidLogicalView"></param>
        ''' <param name="pbstrPhysicalView"></param>
        ''' <remarks></remarks>
        Public Function MapLogicalView(
                                  ByRef rguidLogicalView As Guid,
                                  ByRef pbstrPhysicalView As String
                                      ) As Integer Implements IVsEditorFactory.MapLogicalView
            pbstrPhysicalView = Nothing
        End Function

        ''' <summary>
        ''' Called by owning site after creation
        ''' </summary>
        ''' <param name="Site"></param>
        Public Function SetSite(
                                Site As IServiceProvider
                               ) As Integer Implements IVsEditorFactory.SetSite
            'This same Site already set?  Or Site not yet initialized (= Nothing)?  If so, NOP.
            If _site Is Site Then Exit Function
            'Site is different - set it
            _site = Site
            If TypeOf Site Is IServiceProvider Then
                _siteProvider = New ServiceProvider(Site)
            Else
                Debug.Fail($"{NameOf(Site)} IsNot {NameOf(IServiceProvider)}")
            End If
        End Function

    End Class

End Namespace
