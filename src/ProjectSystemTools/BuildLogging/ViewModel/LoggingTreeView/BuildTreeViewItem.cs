using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.LoggingTreeView
{
    internal sealed class BuildTreeViewItem : INotifyPropertyChanged
    {
        public ObservableCollection<LogTreeViewItem> Children { get; }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public BuildOperation Operation;

        public DateTime? CompletionTime;
        private string _text;

        public event PropertyChangedEventHandler PropertyChanged;

        public BuildTreeViewItem(BuildOperation buildOperation)
        {
            Operation = buildOperation;
            Text = $"{buildOperation} (Running...)";
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
