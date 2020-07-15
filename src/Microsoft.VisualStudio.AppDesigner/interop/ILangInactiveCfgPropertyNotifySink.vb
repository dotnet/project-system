' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
