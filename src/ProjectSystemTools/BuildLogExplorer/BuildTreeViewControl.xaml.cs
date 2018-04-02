using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer
{
    internal partial class BuildTreeViewControl
    {
        public BuildTreeViewControl(ObservableCollection<LogViewModel> logs)
        {
            InitializeComponent();
            DataContext = new RootViewModel(logs);
        }
    }
}