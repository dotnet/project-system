// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class TaskListViewModel
    {
        public string Name { get; }
        public string SourceFilePath { get; }
        public int Number { get; }
        public TimeSpan Time { get; }
        public double Percentage { get; }

        public TaskListViewModel(string name, string sourceFilePath, int number, TimeSpan time, double percentage)
        {
            Name = name;
            SourceFilePath = sourceFilePath;
            Number = number;
            Time = time;
            Percentage = percentage;
        }
    }
}
