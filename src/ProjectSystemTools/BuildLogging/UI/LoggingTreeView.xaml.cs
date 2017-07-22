// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal partial class LoggingTreeView : TreeView
    {
        public LoggingTreeView()
        {
            InitializeComponent();
            RegisterCommandBindings();
        }

        private void RegisterCommandBindings()
        {
            var handlers = new BaseCommandHandler[]
            {
                new OpenCommandHandler() 
            };

            CommandBindings.AddRange(handlers.Select(h => h.CreateCommandBinding()).ToArray());
         }

        private void LoggingTreeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Handle double click directly
            if (e.ClickCount != 2 || !(((StackPanel) sender).DataContext is ProjectTreeViewModel currentItem))
            {
                return;
            }

            // Defer the NavigateTo until after the mouse left is completed, otherwise
            // focus gets forced back to the tree view
            void Action() => currentItem.Open();

            Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action) Action);

            e.Handled = true;
        }
    }
}
