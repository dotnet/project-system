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
