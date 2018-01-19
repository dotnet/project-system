using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class RootViewModel
    {
        public ObservableCollection<LogViewModel> Children { get; }

        public RootViewModel(ObservableCollection<LogViewModel> logs)
        {
            Children = logs;
        }
    }
}
