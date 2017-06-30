//-------------------------------------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//-------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.LoggingTreeView;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.BuildLoggingToolWindow
{
    internal sealed class ToolWindow
    {
        public ObservableCollection<BuildTreeViewItem> BuildItems { get; }

        public ToolWindow()
        {
            BuildItems = new ObservableCollection<BuildTreeViewItem>();
        }
    }

}
