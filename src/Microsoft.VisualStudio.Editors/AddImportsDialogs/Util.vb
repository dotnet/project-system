' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AddImports
    Friend Module Util
        Public Sub SetNavigationInfo(c As Control, nextControl As Control, previousControl As Control)
            c.Tag = New ControlNavigationInfo(nextControl, previousControl)
        End Sub
    End Module
End Namespace
