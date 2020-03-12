' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Class ControlNavigationInfo
        Public ReadOnly NextControl As Control
        Public ReadOnly PreviousControl As Control

        Public Sub New(NextControl As Control, PreviousControl As Control)
            Me.NextControl = NextControl
            Me.PreviousControl = PreviousControl
        End Sub
    End Class
End Namespace