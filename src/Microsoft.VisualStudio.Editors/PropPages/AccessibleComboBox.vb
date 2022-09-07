' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace PropPages

    ' This class wraps ComboBox, overriding the OnSelectedIndexChanged event to enable change notifications for comboboxes in legacy pages.
    ' Must be used instead of ComboBox for legacy prop pages
    Public Class AccessibleComboBox
        Inherits ComboBox

        ' workaround for https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1576845/
        ' raises automation notification on net4.8-enabled VS instances, as *something* (likely in winforms) is swallowing these 
        ' change events
        Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)
            MyBase.OnSelectedIndexChanged(e)
            ' invokes RaiseAutomationNotification adding a notification of type ActionCompleted (2) with notification processing order
            ' CurrentThenMostRecent (4)
            AccessibilityObject.GetType().GetMethod("RaiseAutomationNotification")?.Invoke(AccessibilityObject, new object() { 2, 4, Items.Item(SelectedIndex) })
        End Sub
    End Class
End NameSpace
