﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport(), ComVisible(False), Guid("79eac9ee-baf9-11ce-8c82-00aa004ba90b"), System.Runtime.InteropServices.InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    Friend Interface IInternetSecurityManager
        <PreserveSig()> Function SetSecuritySite() As Integer
        <PreserveSig()> Function GetSecuritySite() As Integer
        <PreserveSig()> Function MapUrlToZone(<[In](), MarshalAs(UnmanagedType.BStr)> url As String, <Out()> ByRef zone As Integer, <[In]()> flags As Integer) As Integer
        <PreserveSig()> Function GetSecurityId() As Integer
        <PreserveSig()> Function ProcessUrlAction(url As String, action As Integer, _
                <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> policy() As Byte, _
                cbPolicy As Integer, ByRef context As Byte, cbContext As Integer, _
                flags As Integer, reserved As Integer) As Integer
        <PreserveSig()> Function QueryCustomPolicy() As Integer
        <PreserveSig()> Function SetZoneMapping() As Integer
        <PreserveSig()> Function GetZoneMappings() As Integer
    End Interface
End Namespace