// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal abstract class BaseCommandHandler
    {
        public CommandBinding CreateCommandBinding()
        {
            return new CommandBinding(
                Command,
                Executed,
                CanExecute
            );
        }

        protected abstract ICommand Command
        {
            get;
        }

        protected abstract void Executed(object sender, ExecutedRoutedEventArgs e);
        protected abstract void CanExecute(object sender, CanExecuteRoutedEventArgs e);
    }
}
