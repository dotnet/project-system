' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.VBRefChangedSvc.Interop

    ''' ;IVbReferenceChangedService
    ''' <summary>
    ''' Interface that defines the contract for VbReferenceChangedService.
    ''' </summary>
    <Guid("B3017D1B-2FF7-4f22-828C-CD74B6A702DC")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    <CLSCompliant(False)>
    <ComImport>
    Friend Interface IVbReferenceChangedService

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig>
        Function ReferenceAdded(
            <[In], MarshalAs(UnmanagedType.IUnknown)> pHierarchy As Object,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyPath As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyName As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyVersion As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyInfo As String
        ) As Integer

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig>
        Function ReferenceRemoved(
            <[In], MarshalAs(UnmanagedType.IUnknown)> pHierarchy As Object,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyPath As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyName As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyVersion As String,
            <[In], MarshalAs(UnmanagedType.BStr)> strAssemblyInfo As String
        ) As Integer

    End Interface

End Namespace

