' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Collections.Immutable
Imports EnvDTE
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.MyApplication
    Friend Class MyApplicationProjectLifetimeTracker
        Implements IVsRunningDocTableEvents
        Implements IVsSolutionEvents

        Private Shared ReadOnly s_instance As MyApplicationProjectLifetimeTracker = New MyApplicationProjectLifetimeTracker

        Private _managerInstances As ImmutableDictionary(Of UInteger, MyApplicationProperties) = ImmutableDictionary(Of UInteger, MyApplicationProperties).Empty
        Private ReadOnly _rdt As RunningDocumentTable
        Private ReadOnly _solution As IVsSolution

        Private Sub New()
            _rdt = New RunningDocumentTable
            _rdt.Advise(Me)

            _solution = DirectCast(ServiceProvider.GlobalProvider.GetService(GetType(IVsSolution)), IVsSolution)
            Dim solutionCookie As UInteger
            _solution.AdviseSolutionEvents(Me, solutionCookie)
        End Sub

        Public Shared Function Track(projectHierarchy As IVsHierarchy) As MyApplicationProperties
            Return s_instance.TrackInternal(projectHierarchy)
        End Function

        Private Function TrackInternal(projectHierarchy As IVsHierarchy) As MyApplicationProperties
            Dim cookie = GetProjectFileCookie(projectHierarchy)

            Dim properties = ImmutableInterlocked.GetOrAdd(_managerInstances, cookie, Function() New MyApplicationProperties())
            ' There is a chance that GetOrAdd will call the valueFactory function when its not needed
            ' and since MyApplicationProperties won't init itself twice, we can just Init it here when we're sure
            properties.Init(projectHierarchy)
            Return properties
        End Function

        Private Function GetProjectFileCookie(projectHierarchy As IVsHierarchy) As UInteger
            Dim hr As Integer
            Dim ExtObject As Object = Nothing
            hr = projectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, ExtObject)
            If NativeMethods.Succeeded(hr) Then
                Dim project = TryCast(ExtObject, Project)
                If project IsNot Nothing Then
                    Return _rdt.GetDocumentInfo(project.FullName).DocCookie
                End If
            End If

        End Function

        Private Function IVsRunningDocTableEvents_OnAfterSave(docCookie As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterSave
            Dim properties As MyApplicationProperties = Nothing
            If _managerInstances.TryGetValue(docCookie, properties) Then
                properties.Save()
            End If

            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnBeforeUnloadProject(pRealHierarchy As IVsHierarchy, pStubHierarchy As IVsHierarchy) As Integer Implements IVsSolutionEvents.OnBeforeUnloadProject
            Dim cookie = GetProjectFileCookie(pRealHierarchy)

            Dim props As MyApplicationProperties = Nothing
            If ImmutableInterlocked.TryRemove(_managerInstances, cookie, props) Then
                props.Close()
            End If

            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnBeforeCloseProject(pHierarchy As IVsHierarchy, fRemoved As Integer) As Integer Implements IVsSolutionEvents.OnBeforeCloseProject
            Return IVsSolutionEvents_OnBeforeUnloadProject(pHierarchy, Nothing)
        End Function

        ' Unused
        Private Function IVsRunningDocTableEvents_OnAfterAttributeChange(docCookie As UInteger, grfAttribs As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterAttributeChange
            Return NativeMethods.S_OK
        End Function

        Private Function IVsRunningDocTableEvents_OnAfterFirstDocumentLock(docCookie As UInteger, dwRDTLockType As UInteger, dwReadLocksRemaining As UInteger, dwEditLocksRemaining As UInteger) As Integer Implements IVsRunningDocTableEvents.OnAfterFirstDocumentLock
            Return NativeMethods.S_OK
        End Function

        Private Function IVsRunningDocTableEvents_OnBeforeLastDocumentUnlock(docCookie As UInteger, dwRDTLockType As UInteger, dwReadLocksRemaining As UInteger, dwEditLocksRemaining As UInteger) As Integer Implements IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock
            Return NativeMethods.S_OK
        End Function

        Private Function IVsRunningDocTableEvents_OnBeforeDocumentWindowShow(docCookie As UInteger, fFirstShow As Integer, pFrame As IVsWindowFrame) As Integer Implements IVsRunningDocTableEvents.OnBeforeDocumentWindowShow
            Return NativeMethods.S_OK
        End Function

        Private Function IVsRunningDocTableEvents_OnAfterDocumentWindowHide(docCookie As UInteger, pFrame As IVsWindowFrame) As Integer Implements IVsRunningDocTableEvents.OnAfterDocumentWindowHide
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnAfterOpenProject(pHierarchy As IVsHierarchy, fAdded As Integer) As Integer Implements IVsSolutionEvents.OnAfterOpenProject
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnQueryCloseProject(pHierarchy As IVsHierarchy, fRemoving As Integer, ByRef pfCancel As Integer) As Integer Implements IVsSolutionEvents.OnQueryCloseProject
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnAfterLoadProject(pStubHierarchy As IVsHierarchy, pRealHierarchy As IVsHierarchy) As Integer Implements IVsSolutionEvents.OnAfterLoadProject
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnQueryUnloadProject(pRealHierarchy As IVsHierarchy, ByRef pfCancel As Integer) As Integer Implements IVsSolutionEvents.OnQueryUnloadProject
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnAfterOpenSolution(pUnkReserved As Object, fNewSolution As Integer) As Integer Implements IVsSolutionEvents.OnAfterOpenSolution
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnQueryCloseSolution(pUnkReserved As Object, ByRef pfCancel As Integer) As Integer Implements IVsSolutionEvents.OnQueryCloseSolution
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnBeforeCloseSolution(pUnkReserved As Object) As Integer Implements IVsSolutionEvents.OnBeforeCloseSolution
            Return NativeMethods.S_OK
        End Function

        Private Function IVsSolutionEvents_OnAfterCloseSolution(pUnkReserved As Object) As Integer Implements IVsSolutionEvents.OnAfterCloseSolution
            Return NativeMethods.S_OK
        End Function
    End Class
End Namespace
