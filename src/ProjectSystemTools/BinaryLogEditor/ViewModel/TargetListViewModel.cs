// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class TargetListViewModel
    {
        public string Name { get; }
        public string SourceFilePath { get; }
        public int Number { get; }
        public TimeSpan Time { get; }
        public double Percentage { get; }

        public TargetListViewModel(string name, string sourceFilePath, int number, TimeSpan time, double percentage)
        {
            Name = name;
            SourceFilePath = sourceFilePath;
            Number = number;
            Time = time;
            Percentage = percentage;
        }
    }
}
