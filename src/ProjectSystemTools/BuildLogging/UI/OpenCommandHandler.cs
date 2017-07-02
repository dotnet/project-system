// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Input;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal sealed class OpenCommandHandler : BaseCommandHandler
    {
        protected override ICommand Command => Commands.Open;

        protected override void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = CanExecute((LoggingTreeView)sender);
        }

        protected override void Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = Executed((LoggingTreeView)sender);
        }

        public static bool CanExecute(LoggingTreeView treeView)
        {
            var treeViewItem = treeView.SelectedItem as LogTreeViewModel;

            return treeViewItem != null;
        }

        public static bool Executed(LoggingTreeView treeView)
        {
            if (!CanExecute(treeView))
            {
                return false;
            }

            ((LogTreeViewModel)treeView.SelectedItem).Open();
            return true;
        }
    }
}
