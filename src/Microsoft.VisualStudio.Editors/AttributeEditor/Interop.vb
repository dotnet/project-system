' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.VBAttributeEditor.Interop

    '--------------------------------------------------------------------------
    ' IVbPermissionSetService:
    '     Interface for the permission set service
    '     Must be kept in sync with its unmanaged version in vbidl.idl
    '--------------------------------------------------------------------------
    <Guid("9DDDA35B-A903-4eca-AAFF-5716AF592D74")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    <CLSCompliant(False)>
    <ComImport>
    Friend Interface IVbPermissionSetService

        <MethodImpl(MethodImplOptions.InternalCall)>
        Function ComputeZonePermissionSet(
            <[In], MarshalAs(UnmanagedType.BStr)> strAppManifestFileName As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strTargetZone As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strExcludedPermissions As String) _
            As <MarshalAs(UnmanagedType.IUnknown)> Object

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig>
        Function IsAvailableInProject(
            <[In], MarshalAs(UnmanagedType.BStr)> strPermissionSet As String,
            <[In], MarshalAs(UnmanagedType.IUnknown)> ProjectPermissionSet As Object,
            <Out, MarshalAs(UnmanagedType.Bool)> ByRef isAvailable As Boolean) _
            As Integer

        ' Returns S_FALSE if there is no tip
        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig>
        Function GetRequiredPermissionsTip(
            <[In], MarshalAs(UnmanagedType.BStr)> strPermissionSet As String,
            <Out, MarshalAs(UnmanagedType.BStr)> ByRef strTip As String) _
            As Integer

    End Interface

End Namespace
