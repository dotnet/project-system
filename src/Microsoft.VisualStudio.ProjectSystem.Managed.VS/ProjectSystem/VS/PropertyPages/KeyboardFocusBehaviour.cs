// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages;

internal static class KeyboardFocusBehaviour
{
    public static readonly DependencyProperty IsKeyboardFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsKeyboardFocused",
            typeof (bool),
            typeof (KeyboardFocusBehaviour),
            new UIPropertyMetadata(false, OnIsKeyboardFocusedPropertyChanged));

    public static bool GetIsKeyboardFocused(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsKeyboardFocusedProperty);
    }

    public static void SetIsKeyboardFocused(DependencyObject obj, bool value)
    {
        obj.SetValue(IsKeyboardFocusedProperty, value);
    }

    private static void OnIsKeyboardFocusedPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && dependencyObject is Control control)
        {
            Keyboard.Focus(control);
        }
    }
}
