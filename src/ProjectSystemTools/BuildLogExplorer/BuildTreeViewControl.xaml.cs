using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer
{
    internal partial class BuildTreeViewControl
    {
        public BuildTreeViewControl()
        {
            InitializeComponent();
        }

        public void SetBuild(LogModel.Build build)
        {
            DataContext = new RootViewModel(build);
        }

        public void SetExceptions(IEnumerable<Exception> exceptions)
        {
            DataContext = new RootViewModel(exceptions);
        }
    }
}