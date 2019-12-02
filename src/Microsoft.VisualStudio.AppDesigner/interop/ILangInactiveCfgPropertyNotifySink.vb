' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.AppDesInterop


    <ComImport, Guid("20bd130e-bcd6-4977-a7da-121555dca33b"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(False), ComVisible(False)>
    Public Interface ILangInactiveCfgPropertyNotifySink

        <PreserveSig>
        Function OnChanged(dispid As Integer, <MarshalAs(UnmanagedType.LPWStr)> wszConfigName As String) As Integer

    End Interface

End Namespace
