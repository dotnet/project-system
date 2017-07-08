// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

        public string FullPath { get; }
        public ImmutableDictionary<string, string> Dimensions { get; }
        public DateTime? StartTime { get; }
        public DateTime? CompletionTime { get; private set; }
        public IReadOnlyList<string> Targets { get; }

        private string BaseText => $"{Path.GetFileName(FullPath)} [{Dimensions.Values.Aggregate((current, next) => $"{current}|{next}")}]";

        public event PropertyChangedEventHandler PropertyChanged;

        public LogTreeViewModel(ConfiguredProject configuredProject, IReadOnlyList<string> targets)
        {
            FullPath = configuredProject.UnconfiguredProject.FullPath;
            Dimensions = configuredProject.ProjectConfiguration.Dimensions.ToImmutableDictionary();
            StartTime = DateTime.Now;
            Targets = targets;
            Text = $"{BaseText} (Building...)";
        }

        public void Completed()
        {
            CompletionTime = DateTime.Now;
            Text = $"{BaseText} ({CompletionTime - StartTime})";
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
