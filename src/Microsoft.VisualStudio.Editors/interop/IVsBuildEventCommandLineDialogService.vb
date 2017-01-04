' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport(), Guid("A0EBEE86-72AD-4a29-8C0E-D745F843BE1D"), _
    InterfaceType(ComInterfaceType.InterfaceIsDual), _
    CLSCompliant(False)> _
    Friend Interface IVsBuildEventCommandLineDialogService
        <PreserveSig()> _
        Function EditCommandLine(<[In](), MarshalAs(UnmanagedType.BStr)> WindowText As String, _
        <[In](), MarshalAs(UnmanagedType.BStr)> HelpID As String, _
        <[In](), MarshalAs(UnmanagedType.BStr)> OriginalCommandLine As String, _
        <[In]()> MacroProvider As IVsBuildEventMacroProvider, _
        <Out(), MarshalAs(UnmanagedType.BStr)> ByRef Result As String) As Integer
    End Interface

End Namespace
