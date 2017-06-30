using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.LoggingTreeView
{
    internal sealed class BuildTreeViewItem
    {
        public ObservableCollection<LogTreeViewItem> Children { get; }

        public string Text { get; }

        public BuildTreeViewItem(BuildOperation buildOperation)
        {
            Text = buildOperation.ToString();
        }
    }
}
