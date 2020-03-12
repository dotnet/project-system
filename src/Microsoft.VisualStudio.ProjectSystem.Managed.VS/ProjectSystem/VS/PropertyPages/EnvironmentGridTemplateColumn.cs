// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class EnvironmentDataGridTemplateColumn : DataGridTemplateColumn
    {
        protected override object? PrepareCellForEdit(FrameworkElement frameworkElement, RoutedEventArgs routedEventArgs)
        {
            if (frameworkElement is ContentPresenter contentPresenter)
            {
                TextBox? textBox = WpfHelper.GetVisualChild<TextBox>(contentPresenter);

                textBox?.SelectAll();
            }

            return null;
        }
    }
}
