' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

'-----------------------------------------------------------------------------
' Refer to vsappid.idl for detail.
'    QueryService(IID_IVsAppId), 
'  srpVsAppId->GetProperty(VSAPROPID_SKUEdition, &varEdition)))
'-----------------------------------------------------------------------------
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.AppDesInterop

    <ComVisible(False),
    ComImport,
    Guid("1EAA526A-0898-11d3-B868-00C04F79F802"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVsAppId

        ' HRESULT SetSite([in] IServiceProvider *pSP);
        Sub SetSite(<MarshalAs(UnmanagedType.Interface)> pSP As OLE.Interop.IServiceProvider)

        ' HRESULT GetProperty([in] VSAPROPID propid,
        '                     [out] VARIANT *pvar);
        <PreserveSig>
        Function GetProperty(propid As Integer,
                         <Out, MarshalAs(UnmanagedType.Struct)> ByRef pvar As Object) As Integer

        ' HRESULT SetProperty([in] VSAPROPID propid,
        '                     [in] VARIANT var);
        Sub SetProperty(propid As Integer,
                         <[In], MarshalAs(UnmanagedType.Struct)> var As Object)

        ' HRESULT GetGuidProperty([in] VSAPROPID propid,
        '                         [out] GUID *pguid);
        Sub GetGuidProperty(propid As Integer,
                             <Out> ByRef pguid As Guid)

        ' HRESULT SetGuidProperty([in] VSAPROPID propid,
        '                         [in] REFGUID rguid);
        Sub SetGuidProperty(propid As Integer, <[In]> ByRef rguid As Guid)

        ' HRESULT Initialize();  ' called after main initialization and before command executing and entering main loop
        Sub Initialize()
    End Interface

End Namespace
