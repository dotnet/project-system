// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class EnvironmentDataGridTemplateColumn : DataGridTemplateColumn
    {
        protected override object PrepareCellForEdit(FrameworkElement frameworkElement, RoutedEventArgs routedEventArgs)
        {
            if (frameworkElement != null && frameworkElement is ContentPresenter)
            {
                var contentPresenter = frameworkElement as ContentPresenter;
                TextBox textBox = WpfHelper.GetVisualChild<TextBox>(contentPresenter);
                if (textBox != null)
                {
                    textBox.SelectAll();
                }
            }
            return null;
        }
    }
}
