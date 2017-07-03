// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel
{
    internal sealed class LogTreeViewModel : INotifyPropertyChanged
    {
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        private string _text;

        public readonly string Path;
        public DateTime? CompletionTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public LogTreeViewModel(ConfiguredProject configuredProject)
        {
            Path = configuredProject.UnconfiguredProject.FullPath;
            Text = $"{Path} (Building...)";
        }

        public void Completed()
        {
            CompletionTime = DateTime.Now;
            Text = $"{Path} ({CompletionTime})";
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Open()
        {
            throw new NotImplementedException();
        }
    }
}
