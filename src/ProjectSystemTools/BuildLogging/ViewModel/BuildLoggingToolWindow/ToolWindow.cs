//-------------------------------------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.LoggingTreeView;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.BuildLoggingToolWindow
{
    internal sealed class ToolWindow : IDisposable
    {
        private readonly BuildLogger _buildLogger;

        public ObservableCollection<BuildTreeViewItem> BuildItems { get; }

        public ToolWindow(BuildLogger buildLogger)
        {
            BuildItems = new ObservableCollection<BuildTreeViewItem>();
            _buildLogger = buildLogger;
            _buildLogger.BuildStarted += OnBuildStarted;
        }

        private void OnBuildStarted(object sender, BuildOperation buildOperation)
        {
            BuildItems.Add(new BuildTreeViewItem(buildOperation));
        }

        public void Dispose()
        {
            _buildLogger.BuildStarted -= OnBuildStarted;
            _buildLogger?.Dispose();
        }
    }

}
