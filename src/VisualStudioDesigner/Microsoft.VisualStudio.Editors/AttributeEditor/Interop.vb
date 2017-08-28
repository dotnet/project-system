' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    <Guid("9DDDA35B-A903-4eca-AAFF-5716AF592D74")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <CLSCompliant(False)> _
    <ComImport()> _
    Friend Interface IVbPermissionSetService

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function ComputeZonePermissionSet( _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAppManifestFileName As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strTargetZone As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strExcludedPermissions As String) _
            As <MarshalAs(UnmanagedType.IUnknown)> Object

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig()> _
        Function IsAvailableInProject( _
            <[In](), MarshalAs(UnmanagedType.BStr)> strPermissionSet As String, _
            <[In](), MarshalAs(UnmanagedType.IUnknown)> ProjectPermissionSet As Object, _
            <Out(), MarshalAs(UnmanagedType.Bool)> ByRef isAvailable As Boolean) _
            As Integer

        ' Returns S_FALSE if there is no tip
        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig()> _
        Function GetRequiredPermissionsTip( _
            <[In](), MarshalAs(UnmanagedType.BStr)> strPermissionSet As String, _
            <Out(), MarshalAs(UnmanagedType.BStr)> ByRef strTip As String) _
            As Integer

    End Interface

End Namespace
