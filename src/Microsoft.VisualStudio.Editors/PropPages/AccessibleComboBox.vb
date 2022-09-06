' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace PropPages

    Public Class AccessibleComboBox
        Inherits ComboBox

        Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)
            MyBase.OnSelectedIndexChanged(e)
            AccessibilityObject.GetType().GetMethod("RaiseAutomationNotification")?.Invoke(AccessibilityObject, new object() {2,4,  Items.Item(SelectedIndex)})
        End Sub
    End Class
End NameSpace
