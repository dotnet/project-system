' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Module Util
        Public Sub SetNavigationInfo(c As Control, nextControl As Control, previousControl As Control)
            c.Tag = New ControlNavigationInfo(nextControl, previousControl)
        End Sub
    End Module
End Namespace
