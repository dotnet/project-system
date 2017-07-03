// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel
{
    internal sealed class BuildTreeViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<LogTreeViewModel> Children { get; }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        private string _text;

        public BuildOperation Operation;
        public DateTime? CompletionTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public BuildTreeViewModel(BuildOperation buildOperation)
        {
            Operation = buildOperation;
            Children = new ObservableCollection<LogTreeViewModel>();
            Text = $"{buildOperation}{(buildOperation == BuildOperation.DesignTime ? "" : " (Running...)")}";
        }

        public void Completed()
        {
            CompletionTime = DateTime.Now;
            Text = $"{Operation} ({CompletionTime})";
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
