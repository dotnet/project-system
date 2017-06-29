// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Input;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.LoggingTreeView;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI.CommandHandlers
{
    internal sealed class Open : BaseHandler
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
            var treeViewItem = treeView.SelectedItem as LogTreeViewItem;

            return treeViewItem != null &&
                   treeViewItem.SupportsNavigateTo;
        }

        public static bool Executed(LoggingTreeView treeView)
        {
            if (CanExecute(treeView))
            {
                ((LogTreeViewItem)treeView.SelectedItem).Open();
                return true;
            }

            return false;
        }
    }
}
