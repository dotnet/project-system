// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Represents an ICommand. Delegates are defined to for the execute and
    /// CanExecute handlers. If no CanExecute is specified it returns true in the CanExecute
    /// method.
    /// </summary>
    internal class RelayCommand : ICommand
    {
        /// <summary>
        /// Creates a new command that is always enabled
        /// </summary>
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }

        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;

        public void RequeryCanExecute()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
