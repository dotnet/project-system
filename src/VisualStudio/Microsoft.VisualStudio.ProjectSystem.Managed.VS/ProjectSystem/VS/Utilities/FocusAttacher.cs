// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
