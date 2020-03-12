' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Public Class HiddenIfMissingPropertyControlData
        Inherits PropertyControlData
        Public Sub New(id As Integer, name As String, formControl As Control)
            MyBase.New(id, name, formControl)
        End Sub

        Public Sub New(id As Integer, name As String, formControl As Control, setter As SetDelegate, getter As GetDelegate, flags As ControlDataFlags, assocControls As Control())
            MyBase.New(id, name, formControl, setter, getter, flags, assocControls)
        End Sub

        Public Overrides Sub InitPropertyValue()
            MyBase.InitPropertyValue()

            If IsMissing Then
                IsHidden = True
            End If
        End Sub
    End Class
End Namespace