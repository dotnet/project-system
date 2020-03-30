' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.VBRefChangedSvc

    ''' ;VBReferenceChangedService
    ''' <summary>
    ''' This service will be called by the VB compiler when a reference change occurred in the VB Project.
    ''' This will initiate the My Extensibility service.
    ''' </summary>
    ''' <remarks>
    ''' - This service is exposed in vbpackage.vb.
    ''' - Registration for this service is in SetupAuthoring\vb\registry\Microsoft.VisualStudio.Editors.vrg_33310.ddr
    '''   and Microsoft.VisualStudio.Editors.vbexpress.vrg_33310.ddr.
    ''' </remarks>
    <CLSCompliant(False)>
    Friend Class VBReferenceChangedService
        Implements Interop.IVbReferenceChangedService

        Public Sub New()
        End Sub

        Private Function ReferenceAdded(
            <[In], MarshalAs(UnmanagedType.IUnknown)> pHierarchy As Object,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyPath As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyName As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyVersion As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyInfo As String
        ) As Integer _
        Implements Interop.IVbReferenceChangedService.ReferenceAdded

            MyExtensibility.MyExtensibilitySolutionService.Instance.ReferenceAdded(
                TryCast(pHierarchy, IVsHierarchy), strAssemblyInfo)

            Return NativeMethods.S_OK
        End Function

        Private Function ReferenceRemoved(
            <[In], MarshalAs(UnmanagedType.IUnknown)> pHierarchy As Object,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyPath As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyName As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyVersion As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyInfo As String
        ) As Integer _
        Implements Interop.IVbReferenceChangedService.ReferenceRemoved

            MyExtensibility.MyExtensibilitySolutionService.Instance.ReferenceRemoved(
                TryCast(pHierarchy, IVsHierarchy), strAssemblyInfo)

            Return NativeMethods.S_OK
        End Function
    End Class

End Namespace

