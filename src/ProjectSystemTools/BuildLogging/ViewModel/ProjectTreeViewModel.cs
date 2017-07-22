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
    internal sealed class ProjectTreeViewModel : INotifyPropertyChanged
    {
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        private string _text;

        public string FullPath { get; }
        public string Configuration { get; }
        public string Platform { get; }
        public DateTime? StartTime { get; }
        public DateTime? CompletionTime { get; private set; }
        public IReadOnlyList<string> Targets { get; }

        private string DimensionText => string.IsNullOrEmpty(Configuration) && string.IsNullOrEmpty(Platform) ?
            "" :
            $" [{Configuration}{(string.IsNullOrEmpty(Configuration) ? "" : "|")}{Platform}]";
        private string BaseText => $"{Path.GetFileName(FullPath)}{DimensionText}";

        public event PropertyChangedEventHandler PropertyChanged;

        public ProjectTreeViewModel(string projectName, string targets, string configuration, string platform, DateTime startTime)
        {
            FullPath = projectName;
            Configuration = configuration;
            Platform = platform;
            StartTime = startTime;
            Targets = targets.Split(';');
            Text = $"{BaseText} (Building...)";
        }

        public void Completed(DateTime completionTime)
        {
            CompletionTime = completionTime;
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
