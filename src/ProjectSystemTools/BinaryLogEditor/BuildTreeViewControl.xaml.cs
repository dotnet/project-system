using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    internal partial class BuildTreeViewControl
    {
        public BuildTreeViewControl(ObservableCollection<BaseViewModel> items)
        {
            InitializeComponent();
            DataContext = new RootViewModel(items);
        }
    }
}