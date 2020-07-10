' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
