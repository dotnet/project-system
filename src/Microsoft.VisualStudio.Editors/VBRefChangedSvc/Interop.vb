' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.VBRefChangedSvc.Interop

    ''' ;IVbReferenceChangedService
    ''' <summary>
    ''' Interface that defines the contract for VbReferenceChangedService.
    ''' </summary>
    <Guid("B3017D1B-2FF7-4f22-828C-CD74B6A702DC")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <CLSCompliant(False)> _
    <ComImport()> _
    Friend Interface IVbReferenceChangedService

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig()> _
        Function ReferenceAdded( _
            <[In](), MarshalAs(UnmanagedType.IUnknown)> pHierarchy As Object, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyPath As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyName As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyVersion As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyInfo As String _
        ) As Integer

        <MethodImpl(MethodImplOptions.InternalCall), PreserveSig()> _
        Function ReferenceRemoved( _
            <[In](), MarshalAs(UnmanagedType.IUnknown)> pHierarchy As Object, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyPath As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyName As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyVersion As String, _
            <[In](), MarshalAs(UnmanagedType.BStr)> strAssemblyInfo As String _
        ) As Integer

    End Interface

End Namespace

