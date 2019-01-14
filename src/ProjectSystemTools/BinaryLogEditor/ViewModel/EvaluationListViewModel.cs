// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class EvaluationListViewModel : IViewModelWithProperties
    {
        private SelectedObjectWrapper _properties;

        public string Name { get; }
        public string Description { get; }
        public string Kind { get; }
        public string SourceFilePath { get; }
        public int Line { get; }
        public int Number { get; }
        public TimeSpan Time { get; }
        public double Percentage { get; }

        public SelectedObjectWrapper Properties => _properties ?? (_properties =
                                                       new SelectedObjectWrapper(
                                                           Name ?? string.Empty,
                                                           Kind,
                                                           null,
                                                           new Dictionary<string, IDictionary<string, string>>
                                                           {
                                                               {
                                                                   "General", new Dictionary<string, string>
                                                                   {
                                                                       {"Description", Description}
                                                                   }
                                                               }
                                                           }));

        public EvaluationListViewModel(string name, string description, string kind, string sourceFilePath, int? line, int number, TimeSpan time, double percentage)
        {
            Name = name;
            Description = description;
            Kind = kind;
            SourceFilePath = sourceFilePath;
            Line = line ?? 0;
            Number = number;
            Time = time;
            Percentage = percentage;
        }
    }
}
