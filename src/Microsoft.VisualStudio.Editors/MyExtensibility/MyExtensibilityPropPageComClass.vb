' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices

Imports Res = My.Resources.MyExtensibilityRes

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' ;MyExtensibilityPropPageComClass
    ''' <summary>
    ''' COM class for My Extensions property page.
    ''' </summary>
    <Guid("F24459FC-E883-4A8E-9DA2-AEF684F0E1F4"),
    ComVisible(True),
    CLSCompliant(False)>
    Public Class MyExtensibilityPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property ControlType() As Type
            Get
                Return GetType(MyExtensibilityPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Windows.Forms.Control
            Return New MyExtensibilityPropPage()
        End Function

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return Res.PropertyPageTab
            End Get
        End Property
    End Class
End Namespace
