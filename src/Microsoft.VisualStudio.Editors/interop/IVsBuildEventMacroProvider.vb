' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport, Guid("ED895476-EF59-46fc-A985-581F58343E61"),
    InterfaceType(ComInterfaceType.InterfaceIsDual),
    CLSCompliant(False)>
    Friend Interface IVsBuildEventMacroProvider
        Function GetCount() As Integer
        Sub GetExpandedMacro(<[In]> Index As Integer,
           <Out, MarshalAs(UnmanagedType.BStr)> ByRef MacroName As String,
           <Out, MarshalAs(UnmanagedType.BStr)> ByRef MacroValue As String)
    End Interface

End Namespace
