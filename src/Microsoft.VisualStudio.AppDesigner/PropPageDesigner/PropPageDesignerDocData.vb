' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop

Imports Win32Constant = Microsoft.VisualStudio.Editors.AppDesInterop.Win32Constant

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' The DocData for the application Designer.
    ''' </summary>
    ''' <remarks>
    ''' The Application Designer does not have a physical file for persistence 
    ''' since it uses the project system directly.  We use this to prevent VS looking for a file
    '''</remarks>
    <ComSourceInterfaces(GetType(IVsTextBufferDataEvents))>
    Public NotInheritable Class PropPageDesignerDocData
        Implements IDisposable
        Implements IVsUserData
        Implements IVsPersistDocData2
        Implements OLE.Interop.IObjectWithSite
        Implements IVsTextBufferProvider

        'Event support for IVsTextBufferDataEvents
        Public Delegate Sub LoadCompletedDelegate(Reload As Integer)
        Public Delegate Sub FileChangedDelegate(ChangeFlags As UInteger, FileAttrs As UInteger)

        Public Event OnLoadCompleted As LoadCompletedDelegate
        Public Event OnFileChanged As FileChangedDelegate

        ' VsTextBuffer class used for providing a textbuffer implementation 
        ' which the IDE needs to operate.  Nothing currently written or read from the 
        ' text stream.
        Private _vsTextBuffer As IVsTextBuffer

        'Service provider members
        Private _baseProvider As IServiceProvider
        Private _siteProvider As IServiceProvider

        ' IVsHierarchy, ItemId, and cookie passed in on registration
        Private _mkDocument As String

        ' Dirty and readonly state
        Private _isReadOnly As Boolean
        Private _isDirty As Boolean

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="BaseProvider"></param>
        Public Sub New(BaseProvider As IServiceProvider)
            'not must init to do here
            _baseProvider = BaseProvider
        End Sub

        ''' <summary>
        ''' Creates VsTextBuffer if necessary and returns the instance of VsTextBuffer
        ''' </summary>
        Private ReadOnly Property VsTextStream As IVsTextBuffer
            Get
                If _vsTextBuffer IsNot Nothing Then
                    Return _vsTextBuffer
                End If

                ' Get the LocalRegistry service and use it to create an instance of the VsTextBuffer class
                Dim localRegistry As ILocalRegistry = Nothing
                If _baseProvider IsNot Nothing Then
                    localRegistry = DirectCast(_baseProvider.GetService(GetType(ILocalRegistry)), ILocalRegistry)
                End If
                If localRegistry Is Nothing Then
                    Throw New COMException(My.Resources.Designer.DFX_NoLocalRegistry, NativeMethods.E_FAIL)
                End If

                'CONSIDER: Need to check with FX team about removing assert in MS.VS.Shell.Design.DesignerWindowPane.RegisterView
                ' If we don't provide VsTextBuffer, we assert over and over again 
                Try
                    Dim guidTemp As Guid = GetType(IVsTextStream).GUID
                    Dim objPtr As IntPtr = IntPtr.Zero
                    VSErrorHandler.ThrowOnFailure(localRegistry.CreateInstance(GetType(VsTextBufferClass).GUID, Nothing, guidTemp, Win32Constant.CLSCTX_INPROC_SERVER, objPtr))

                    If Not objPtr.Equals(IntPtr.Zero) Then
                        _vsTextBuffer = CType(Marshal.GetObjectForIUnknown(objPtr), IVsTextStream)
                        Marshal.Release(objPtr)

                        Dim ows As OLE.Interop.IObjectWithSite = TryCast(_vsTextBuffer, OLE.Interop.IObjectWithSite)
                        If ows IsNot Nothing Then
                            Dim sp As OLE.Interop.IServiceProvider = TryCast(_baseProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                            Debug.Assert(sp IsNot Nothing, "Expected to get a native service provider from our managed service provider")

                            If sp IsNot Nothing Then
                                ows.SetSite(sp)
                            End If
                        End If
                    End If
                Catch ex As Exception
                    Throw New COMException(My.Resources.Designer.DFX_UnableCreateTextBuffer, NativeMethods.E_FAIL)
                End Try
                Return _vsTextBuffer
            End Get
        End Property

#Region "IVsUserData"
        ''' <summary>
        ''' Gets docdata specific data based on guid
        ''' </summary>
        ''' <param name="riidKey"></param>
        ''' <param name="pvtData"></param>
        Public Function GetData(ByRef riidKey As Guid, ByRef pvtData As Object) As Integer Implements IVsUserData.GetData
            If riidKey.Equals(GetType(IVsUserData).GUID) Then
                'IID_IVsUserData (GUID_VsBufferMoniker) is the guid used for retrieving MkDocument (filename)
                Return NativeMethods.S_OK
                pvtData = _mkDocument
                Return NativeMethods.S_OK
            ElseIf _vsTextBuffer IsNot Nothing Then
                Return CType(_vsTextBuffer, IVsUserData).GetData(riidKey, pvtData)
            Else
                Return NativeMethods.E_FAIL
            End If
        End Function

        ''' <summary>
        ''' Sets docdata specific data using guid key
        ''' </summary>
        ''' <param name="riidKey"></param>
        ''' <param name="vtData"></param>
        Public Function SetData(ByRef riidKey As Guid, vtData As Object) As Integer Implements IVsUserData.SetData
            If _vsTextBuffer IsNot Nothing Then
                Return CType(_vsTextBuffer, IVsUserData).SetData(riidKey, vtData)
            Else
                Return NativeMethods.E_FAIL
            End If
        End Function
#End Region

#Region "IVsPersistDocData2 implementation"
#Region "IVsPersistDocData implementation"
        'The IVsPersistDocData2 inherits from IVsPersistDocData
        'The compiler expects both interfaces to be implemented
        'Whether this is a bug or not has yet to be determined
        'It may having something to do with it being defined in an interop assembly.

        Public Function Close() As Integer Implements IVsPersistDocData.Close
            Return Close2()
        End Function

        Public Function GetGuidEditorType(ByRef pClassID As Guid) As Integer Implements IVsPersistDocData.GetGuidEditorType
            Return GetGuidEditorType2(pClassID)
        End Function

        Public Function IsDocDataDirty(ByRef pfDirty As Integer) As Integer Implements IVsPersistDocData.IsDocDataDirty
            Return IsDocDataDirty2(pfDirty)
        End Function

        Public Function IsDocDataReloadable(ByRef pfReloadable As Integer) As Integer Implements IVsPersistDocData.IsDocDataReloadable
            Return IsDocDataReloadable2(pfReloadable)
        End Function

        Public Function LoadDocData(pszMkDocument As String) As Integer Implements IVsPersistDocData.LoadDocData
            Return LoadDocData2(pszMkDocument)
        End Function

        Public Function OnRegisterDocData(docCookie As UInteger, pHierNew As IVsHierarchy, itemidNew As UInteger) As Integer Implements IVsPersistDocData.OnRegisterDocData
            Return OnRegisterDocData2(docCookie, pHierNew, itemidNew)
        End Function

        Public Function ReloadDocData(grfFlags As UInteger) As Integer Implements IVsPersistDocData.ReloadDocData
            Return ReloadDocData2(grfFlags)
        End Function

        Public Function RenameDocData(grfAttribs As UInteger, pHierNew As IVsHierarchy, itemidNew As UInteger, pszMkDocumentNew As String) As Integer Implements IVsPersistDocData.RenameDocData
            Return RenameDocData2(grfAttribs, pHierNew, itemidNew, pszMkDocumentNew)
        End Function

        Public Function SaveDocData(dwSave As VSSAVEFLAGS, ByRef pbstrMkDocumentNew As String, ByRef pfSaveCanceled As Integer) As Integer Implements IVsPersistDocData.SaveDocData
            Return SaveDocData2(dwSave, pbstrMkDocumentNew, pfSaveCanceled)
        End Function

        Public Function SetUntitledDocPath(pszDocDataPath As String) As Integer Implements IVsPersistDocData.SetUntitledDocPath
            Return SetUntitledDocPath2(pszDocDataPath)
        End Function
#End Region
        Public Function Close2() As Integer Implements IVsPersistDocData2.Close
            Dispose(True)
        End Function

        Public Function GetGuidEditorType2(ByRef pClassID As Guid) As Integer Implements IVsPersistDocData2.GetGuidEditorType
            pClassID = PropPageDesignerEditorFactory.EditorGuid
        End Function

        Public Function IsDocDataDirty2(ByRef pfDirty As Integer) As Integer Implements IVsPersistDocData2.IsDocDataDirty
            If _isDirty Then
                pfDirty = 1
            Else
                pfDirty = 0
            End If
        End Function

        Public Function IsDocDataReadOnly(ByRef pfReadOnly As Integer) As Integer Implements IVsPersistDocData2.IsDocDataReadOnly
            If _isReadOnly Then
                pfReadOnly = 1
            Else
                pfReadOnly = 0
            End If
        End Function

        Public Function IsDocDataReloadable2(ByRef pfReloadable As Integer) As Integer Implements IVsPersistDocData2.IsDocDataReloadable
            pfReloadable = 0
        End Function

        Public Function LoadDocData2(pszMkDocument As String) As Integer Implements IVsPersistDocData2.LoadDocData
            'Nothing to do here, no real file to load
            _mkDocument = pszMkDocument
            RaiseEvent OnLoadCompleted(0) 'FALSE == 0
        End Function

        Public Function OnRegisterDocData2(docCookie As UInteger, pHierNew As IVsHierarchy, itemidNew As UInteger) As Integer Implements IVsPersistDocData2.OnRegisterDocData
        End Function

        Public Function ReloadDocData2(grfFlags As UInteger) As Integer Implements IVsPersistDocData2.ReloadDocData
            'Should we reload anything?
            RaiseEvent OnLoadCompleted(1) 'TRUE == 1
        End Function

        Public Function RenameDocData2(grfAttribs As UInteger, pHierNew As IVsHierarchy, itemidNew As UInteger, pszMkDocumentNew As String) As Integer Implements IVsPersistDocData2.RenameDocData
            Return VSConstants.E_NOTIMPL
        End Function

        Public Function SaveDocData2(dwSave As VSSAVEFLAGS, ByRef pbstrMkDocumentNew As String, ByRef pfSaveCanceled As Integer) As Integer Implements IVsPersistDocData2.SaveDocData
            pfSaveCanceled = 0
            'Nothing to do since we have no true file backing
        End Function

        Public Function SetDocDataDirty(fDirty As Integer) As Integer Implements IVsPersistDocData2.SetDocDataDirty
            If fDirty <> 0 Then
                _isDirty = True
            Else
                _isDirty = False
            End If
        End Function

        Public Function SetDocDataReadOnly(fReadOnly As Integer) As Integer Implements IVsPersistDocData2.SetDocDataReadOnly
            If fReadOnly <> 0 Then
                _isReadOnly = True
            Else
                _isReadOnly = False
            End If
            Return VSConstants.S_OK
        End Function

        Public Function SetUntitledDocPath2(pszDocDataPath As String) As Integer Implements IVsPersistDocData2.SetUntitledDocPath
            Return VSConstants.E_NOTIMPL
        End Function
#End Region

#Region "IVsTextBufferProvider"
        ''' <summary>
        ''' Returns the IVsTextLines for our virtual buffer
        ''' </summary>
        ''' <param name="ppTextBuffer"></param>
        Public Function GetTextBuffer(ByRef ppTextBuffer As IVsTextLines) As Integer Implements IVsTextBufferProvider.GetTextBuffer
            If TypeOf VsTextStream Is IVsTextLines Then
                ppTextBuffer = CType(VsTextStream, IVsTextLines)
            Else
                ppTextBuffer = Nothing
            End If
        End Function

        ''' <summary>
        ''' Locks/Unlocks our buffer
        ''' </summary>
        ''' <param name="fLock"></param>
        Public Function LockTextBuffer(fLock As Integer) As Integer Implements IVsTextBufferProvider.LockTextBuffer
            If fLock = 0 Then
                Return VsTextStream.UnlockBuffer()
            Else
                Return VsTextStream.LockBuffer()
            End If
        End Function

        ''' <summary>
        ''' SetTextBuffer is currently unsupported.
        ''' </summary>
        ''' <param name="pTextBuffer"></param>
        Public Function SetTextBuffer(pTextBuffer As IVsTextLines) As Integer Implements IVsTextBufferProvider.SetTextBuffer
            Debug.Fail("SetTextBuffer not supported in Application Designer!")
        End Function
#End Region

#Region "OLE.Interop.IObjectWithSite"
        ''' <summary>
        ''' Returns the current hosting site for the DocData
        ''' </summary>
        ''' <param name="riid"></param>
        ''' <param name="ppvSite"></param>
        Public Sub GetSite(ByRef riid As Guid, ByRef ppvSite As IntPtr) Implements OLE.Interop.IObjectWithSite.GetSite
            Dim punk As IntPtr = Marshal.GetIUnknownForObject(_siteProvider)
            Dim hr As Integer
            hr = Marshal.QueryInterface(punk, riid, ppvSite)
            Marshal.Release(punk)
            If NativeMethods.Failed(hr) Then
                Marshal.ThrowExceptionForHR(hr)
            End If
        End Sub

        ''' <summary>
        ''' Sets the hosting site for the DocData
        ''' </summary>
        ''' <param name="pUnkSite"></param>
        Public Sub SetSite(pUnkSite As Object) Implements OLE.Interop.IObjectWithSite.SetSite
            If TypeOf pUnkSite Is OLE.Interop.IServiceProvider Then
                _siteProvider = New Shell.ServiceProvider(DirectCast(pUnkSite, OLE.Interop.IServiceProvider))
            Else
                _siteProvider = Nothing
            End If
        End Sub
#End Region

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Disposes of any the doc data
        ''' </summary>
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub

        ''' <summary>
        ''' Disposes of contained objects
        ''' </summary>
        ''' <param name="disposing"></param>
        Private Overloads Sub Dispose(disposing As Boolean)
            If disposing Then
                ' Dispose managed resources.
                _baseProvider = Nothing

                If _vsTextBuffer IsNot Nothing Then
                    ' Close IVsPersistDocData
                    Dim docData As IVsPersistDocData = TryCast(_vsTextBuffer, IVsPersistDocData)
                    If docData IsNot Nothing Then
                        docData.Close()
                    End If
                    _vsTextBuffer = Nothing
                End If
            End If
            ' Call the appropriate methods to clean up 
            ' unmanaged resources here.
            ' If disposing is false, 
            ' only the following code is executed.

        End Sub

#End Region

    End Class

End Namespace

