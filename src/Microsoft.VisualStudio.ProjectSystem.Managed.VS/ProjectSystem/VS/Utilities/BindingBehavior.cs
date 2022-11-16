// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

internal static class BindingBehavior
{
    public static readonly DependencyProperty UpdatePropertyOnEnterPressedProperty
        = DependencyProperty.RegisterAttached(
            "UpdatePropertyOnEnterPressed",
            typeof(DependencyProperty),
            typeof(BindingBehavior),
            new System.Windows.PropertyMetadata(null, OnUpdatePropertyOnEnterPressedPropertyChanged));

    public static void SetUpdatePropertyOnEnterPressed(DependencyObject dp, DependencyProperty value)
    {
        dp.SetValue(UpdatePropertyOnEnterPressedProperty, value);
    }

    public static DependencyProperty? GetUpdatePropertyOnEnterPressed(DependencyObject dp)
    {
        return (DependencyProperty?)dp.GetValue(UpdatePropertyOnEnterPressedProperty);
    }

    private static void OnUpdatePropertyOnEnterPressedPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is UIElement element)
        {
            switch (e.OldValue, e.NewValue)
            {
                case (null, not null):
                {
                    element.PreviewKeyDown += OnPreviewKeyDown;
                    break;
                }

                case (not null, null):
                {
                    element.PreviewKeyDown -= OnPreviewKeyDown;
                    break;
                }
            }
        }

        return;

        static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DependencyProperty? property = GetUpdatePropertyOnEnterPressed((DependencyObject)e.Source);

                if (property is not null && e.Source is DependencyObject o)
                {
                    BindingExpression? binding = BindingOperations.GetBindingExpression(o, property);

                    binding?.UpdateSource();
                }
            }
        }
    }
}
