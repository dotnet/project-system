// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    public class FocusAttacher
    {
        public static readonly DependencyProperty FocusProperty = DependencyProperty.RegisterAttached("Focus", typeof(bool), typeof(FocusAttacher), new PropertyMetadata(false, FocusChanged));
        public static bool GetFocus(DependencyObject d)
        {
            return (bool)d.GetValue(FocusProperty);
        }

        public static void SetFocus(DependencyObject d, bool value)
        {
            d.SetValue(FocusProperty, value);
        }

        private static void FocusChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((UIElement)sender).Focus();
                if (sender is TextBox tb)
                {
                    tb.SelectAll();
                }
            }
        }
    }
}
