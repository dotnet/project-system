' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport, Guid("A0EBEE86-72AD-4a29-8C0E-D745F843BE1D"),
    InterfaceType(ComInterfaceType.InterfaceIsDual),
    CLSCompliant(False)>
    Friend Interface IVsBuildEventCommandLineDialogService
        <PreserveSig>
        Function EditCommandLine(<[In], MarshalAs(UnmanagedType.BStr)> WindowText As String,
        <[In], MarshalAs(UnmanagedType.BStr)> HelpID As String,
        <[In], MarshalAs(UnmanagedType.BStr)> OriginalCommandLine As String,
        <[In]> MacroProvider As IVsBuildEventMacroProvider,
        <Out, MarshalAs(UnmanagedType.BStr)> ByRef Result As String) As Integer
    End Interface

End Namespace
