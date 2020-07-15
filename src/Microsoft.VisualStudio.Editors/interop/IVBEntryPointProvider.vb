' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport, Guid("3EB048DA-F881-4a7f-A9D4-0258E19978AA"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(False)>
    Friend Interface IVBEntryPointProvider
        'Lists all Form classes with an entry point. If called with cItems = 0 and
        '  pcActualItems != NULL, GetEntryPointsList returns in pcActualItems the number
        '  of items available. When called with cItems != 0, GetEntryPointsList assumes
        '  that there is enough space in strList[] for that many items, and fills up the
        '  array with those items (up to maximum available).  Returns in pcActualItems 
        '  the actual number of items that could be put in the array (this can be greater than or 
        '  less than cItems). Assumes that the caller takes care of array allocation and de-allocation.
        '        Function GetFormEntryPointsList(pHierarchy As Object, _
        '                                       cItems As UInteger, _
        '                                      <MarshalAs(UnmanagedType.LPArray, arraysubtype:=UnmanagedType.BStr), [In](), Out()> ByRef c() As String, _
        '                                     <Out()> ByRef pcActualItems As UInteger) As Integer
        Function GetFormEntryPointsList(<MarshalAs(UnmanagedType.IUnknown), [In]> pHierarchy As Object,
                                        cItems As UInteger,
                                        <Out, MarshalAs(UnmanagedType.LPArray)> bstrList As String(),
                                        <Out> ByRef pcActualItems As UInteger) As Integer

    End Interface

End Namespace
