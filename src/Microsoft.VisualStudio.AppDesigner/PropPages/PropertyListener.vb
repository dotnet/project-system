' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Shell.Interop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Listens for property changes on a particular object.
    ''' </summary>
    Public Class PropertyListener
        Implements OLE.Interop.IPropertyNotifySink
        Implements ILangInactiveCfgPropertyNotifySink
        Implements IDisposable

        Private _cookieActiveCfg As NativeMethods.ConnectionPointCookie 'The connection cookie for IPropertyNotifySink
        Private _cookieInactiveCfg As NativeMethods.ConnectionPointCookie 'The connection cookie for ILangInactiveCfgPropertyNotifySink
        Private ReadOnly _propPage As PropPageUserControlBase 'The property page to notify when a property changes

#If DEBUG Then
        Private ReadOnly _debugSourceName As String 'For debugging purposes: name of the properties object that is being listened to
#End If

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="PropPage">The property page to notify when a property changes</param>
        ''' <param name="EventSource">The object to try listening to property changes on.</param>
        ''' <param name="DebugSourceName">For debugging purposes: name of the properties object that is being listened to.</param>
        Private Sub New(PropPage As PropPageUserControlBase, EventSource As Object, DebugSourceName As String)
            _propPage = PropPage
#If DEBUG Then
            _debugSourceName = DebugSourceName
#End If
        End Sub

        ''' <summary>
        ''' Attempts to constructor a listener for the given property change event source, ignoring
        '''   any failures.
        ''' </summary>
        ''' <param name="PropPage">The property page to notify when a property changes</param>
        ''' <param name="EventSource">The object to try listening to property changes on.</param>
        ''' <param name="DebugSourceName">A debug name for the source</param>
        ''' <param name="ProjectHierarchy">The project's IVsHierarchy</param>
        ''' <param name="ListenToInactiveConfigs">If true, we attempt to listen to both the active and inactive configurations for property changes.
        '''   This is not necessary or possible for non-configuration-specific pages - we always listen to common property changes.
        ''' </param>
        ''' <returns>If it succeeds, a valid listener is created.  If it fails, Nothing is returned.</returns>
        Public Shared Function TryCreate(PropPage As PropPageUserControlBase, EventSource As Object, DebugSourceName As String, ProjectHierarchy As IVsHierarchy, ListenToInactiveConfigs As Boolean) As PropertyListener
            Debug.Assert(ProjectHierarchy IsNot Nothing)
            Common.Switches.TracePDProperties(TraceLevel.Info, "Attempting to hook up IPropertyNotifySink to object '" & DebugSourceName & "' of type " & TypeName(EventSource))

            Dim vsCfg = TryCast(EventSource, IVsCfg)
            If vsCfg IsNot Nothing Then
                'We need to get an IDispatch for the configuration, which we can do through IVsExtensibleObject off
                '  of the configuration provider of the project.
                'From there, we can QI for ProjectConfigurationProperties, which implements IConnectionPointContainer for IPropertyNotifySink
                Dim VsCfgProviderObject As Object = Nothing
                If VSErrorHandler.Succeeded(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, VsCfgProviderObject)) AndAlso VsCfgProviderObject IsNot Nothing Then
                    Dim VsExtensibleObject As IVsExtensibleObject = TryCast(VsCfgProviderObject, IVsExtensibleObject)
                    Debug.Assert(VsExtensibleObject IsNot Nothing, "Expected IVsCfgProvider object to implement IVsExtensibleObject")
                    If VsExtensibleObject IsNot Nothing Then
                        Dim Dispatch As Object = Nothing
                        Dim ConfigFullName As String = Nothing
                        vsCfg.get_DisplayName(ConfigFullName)
                        Debug.Assert(ConfigFullName <> "", "Unable to get display name of config")
                        If ConfigFullName <> "" Then
                            VsExtensibleObject.GetAutomationObject(ConfigFullName, Dispatch)
                            Debug.Assert(Dispatch IsNot Nothing, "Couldn't get automation object for configuration")
                            Dim ConfigProperties As VSLangProj.ProjectConfigurationProperties = TryCast(Dispatch, VSLangProj.ProjectConfigurationProperties)
                            Debug.Assert(ConfigProperties IsNot Nothing OrElse CpsPropertyDescriptorWrapper.IsCpsComponent(Dispatch), "Couldn't get ProjectConfigurationProperties from config")
                            If ConfigProperties IsNot Nothing Then
                                EventSource = ConfigProperties
                            End If
                        End If
                    End If
                Else
                    Debug.Fail("Unable to get IVsCfgProvider from project")
                End If
            End If

            If SupportsConnectionPointContainer(EventSource) Then
                Dim CookieActiveCfg As NativeMethods.ConnectionPointCookie = Nothing
                Dim CookieInactiveCfg As NativeMethods.ConnectionPointCookie = Nothing
                Try
                    Dim Listener As New PropertyListener(PropPage, EventSource, DebugSourceName)

                    CookieActiveCfg = New NativeMethods.ConnectionPointCookie(EventSource, Listener, GetType(OLE.Interop.IPropertyNotifySink))
                    Listener._cookieActiveCfg = CookieActiveCfg
                    CookieActiveCfg = Nothing
                    Common.Switches.TracePDProperties(TraceLevel.Info, "... Succeeded for the active configuration (or for common properties)")

                    If ListenToInactiveConfigs Then
                        Try
                            CookieInactiveCfg = New NativeMethods.ConnectionPointCookie(EventSource, Listener, GetType(ILangInactiveCfgPropertyNotifySink))
                            Listener._cookieInactiveCfg = CookieInactiveCfg
                            CookieInactiveCfg = Nothing
                            Common.Switches.TracePDProperties(TraceLevel.Info, "... Succeeded for inactive configurations")
                        Catch ex As Exception When Common.ReportWithoutCrash(ex, "Unable to get connection point cookie for ILangInactiveCfgPropertyNotifySink", NameOf(PropertyListener))
                            'We ignore if this happens
                            Common.Switches.TracePDProperties(TraceLevel.Info, "...  Exception thrown for inactive configurations: " & ex.Message)
                        End Try
                    End If

                    Return Listener
                Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(TryCreate), NameOf(PropertyListener))
                    Common.Switches.TracePDProperties(TraceLevel.Info, "...  Exception thrown: " & ex.ToString)
                Finally
                    If CookieActiveCfg IsNot Nothing Then
                        CookieActiveCfg.Disconnect()
                    End If
                    If CookieInactiveCfg IsNot Nothing Then
                        CookieInactiveCfg.Disconnect()
                    End If
                End Try
            Else
                Common.Switches.TracePDProperties(TraceLevel.Info, "...  Not supported")
            End If

            Return Nothing
        End Function

#Region " IDisposable Support "

        ''' <summary>
        ''' IDisposable support
        ''' </summary>
        ''' <param name="Disposing"></param>
        Private Overloads Sub Dispose(Disposing As Boolean)
            If Disposing Then
                If _cookieActiveCfg IsNot Nothing Then
                    _cookieActiveCfg.Disconnect()
                    _cookieActiveCfg = Nothing
                End If
                If _cookieInactiveCfg IsNot Nothing Then
                    _cookieInactiveCfg.Disconnect()
                    _cookieInactiveCfg = Nothing
                End If
            Else
                Debug.Assert(_cookieActiveCfg Is Nothing, "PropertyListener didn't get disposed")
                Debug.Assert(_cookieInactiveCfg Is Nothing, "PropertyListener didn't get disposed")
            End If
        End Sub

        ''' <summary>
        ''' IDisposable support
        ''' </summary>
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' IDisposable support
        ''' </summary>
        Protected Overrides Sub Finalize()
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(False)
            MyBase.Finalize()
        End Sub

#End Region

        ''' <summary>
        ''' Returns true iff the given source supports IConnectionPointContainer
        ''' </summary>
        ''' <param name="EventSource"></param>
        Private Shared Function SupportsConnectionPointContainer(EventSource As Object) As Boolean
            If EventSource IsNot Nothing Then
                If TypeOf EventSource Is OLE.Interop.IConnectionPointContainer OrElse TypeOf EventSource Is ComTypes.IConnectionPointContainer Then
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Notifies a sink that the [bindable] property specified by dispID has changed. If dispID is 
        '''   DISPID_UNKNOWN, then multiple properties have changed together. The client (owner of the sink) 
        '''   should then retrieve the current value of each property of interest from the object that 
        '''   generated the notification.
        ''' </summary>
        ''' <param name="DISPID">[in] Dispatch identifier of the property that changed, or DISPID_UNKNOWN if multiple properties have changed.</param>
        ''' <remarks>
        ''' S_OK is returned in all cases even when the sink does not need [bindable] properties or when some 
        '''   other failure has occurred. In short, the calling object simply sends the notification and cannot 
        '''   attempt to use an error code (such as E_NOTIMPL) to determine whether to not send the notification 
        '''   in the future. Such semantics are not part of this interface.
        ''' </remarks>
        Private Sub OnChanged(DISPID As Integer) Implements OLE.Interop.IPropertyNotifySink.OnChanged
            Dim DebugSourceName As String = Nothing
#If DEBUG Then
            DebugSourceName = _debugSourceName
#End If
            Debug.Assert(_propPage IsNot Nothing)
            _propPage.OnExternalPropertyChanged(DISPID, DebugSourceName)
        End Sub

        ''' <summary>
        ''' Notifies a sink that a [requestedit] property is about to change and that the object is asking the sink how to proceed.
        ''' </summary>
        ''' <param name="DISPID"></param>
        ''' <remarks>
        ''' S_OK - The specified property or properties are allowed to change. 
        ''' S_FALSE - The specified property or properties are not allowed to change. The caller must obey this return value by discarding the new property value(s). This is part of the contract of the [requestedit] attribute and this method. 
        ''' 
        ''' The sink may choose to allow or disallow the change to take place. For example, the sink may enforce a read-only state 
        '''   on the property. DISPID_UNKNOWN is a valid parameter to this method to indicate that multiple properties are about to 
        '''   change. In this case, the sink can enforce a global read-only state for all [requestedit] properties in the object, 
        '''   including any specific ones that the sink otherwise recognizes.
        '''
        ''' If the sink allows changes, the object must also make IPropertyNotifySink::OnChanged notifications for any properties 
        '''   that are marked [bindable] in addition to [requestedit].
        '''
        ''' This method cannot be used to implement any sort of data validation. At the time of the call, the desired new value of 
        '''   the property is unavailable and thus cannot be validated. This method's only purpose is to allow the sink to enforce 
        '''   a read-only state on a property.
        ''' </remarks>
        Private Sub OnRequestEdit(DISPID As Integer) Implements OLE.Interop.IPropertyNotifySink.OnRequestEdit
            Dim DebugSourceName As String = Nothing
#If DEBUG Then
            DebugSourceName = _debugSourceName
#End If
            _propPage.OnExternalPropertyRequestEdit(DISPID, DebugSourceName)
        End Sub

        ''' <summary>
        ''' Notification that a property has changed on a configuration that is not currently active.
        ''' </summary>
        ''' <param name="dispid"></param>
        ''' <param name="wszConfigName"></param>
        Public Function OnChanged(dispid As Integer, wszConfigName As String) As Integer Implements ILangInactiveCfgPropertyNotifySink.OnChanged
            Dim DebugSourceName As String = Nothing
#If DEBUG Then
            DebugSourceName = "[Inactive Config '" & wszConfigName & "'] : " & _debugSourceName
#End If
            _propPage.OnExternalPropertyChanged(dispid, DebugSourceName)
        End Function
    End Class

End Namespace
