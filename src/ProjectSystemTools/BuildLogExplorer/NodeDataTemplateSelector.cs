// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer
{
    public sealed class NodeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement frameworkElement)
            {
                if (item is NodeViewModel nodeViewModel)
                {
                    switch (nodeViewModel.Result)
                    {
                        case Result.Failed:
                            return frameworkElement.FindResource("FailedDataTemplate") as DataTemplate;

                        case Result.Succeeded:
                            return frameworkElement.FindResource("SucceededDataTemplate") as DataTemplate;

                        case Result.Skipped:
                            return frameworkElement.FindResource("SkippedDataTemplate") as DataTemplate;
                    }
                }

                return frameworkElement.FindResource("BaseDataTemplate") as DataTemplate;
            }

            return null;
        }
    }
}
