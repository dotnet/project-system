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
        private BuildTreeViewItem _currentBuildItem;

        public ObservableCollection<BuildTreeViewItem> BuildItems { get; }

        public ToolWindow(BuildLogger buildLogger)
        {
            BuildItems = new ObservableCollection<BuildTreeViewItem>();
            _buildLogger = buildLogger;
            _buildLogger.BuildStarted += OnBuildStarted;
            _buildLogger.BuildEnded += OnBuildEnded;
        }

        private void OnBuildEnded(object sender, BuildOperation e)
        {
            _currentBuildItem?.Completed();
            _currentBuildItem = null;
        }

        private void OnBuildStarted(object sender, BuildOperation buildOperation)
        {
            _currentBuildItem = new BuildTreeViewItem(buildOperation);
            BuildItems.Add(_currentBuildItem);
        }

        public void Dispose()
        {
            _buildLogger.BuildStarted -= OnBuildStarted;
            _buildLogger.BuildEnded -= OnBuildEnded;
            _buildLogger?.Dispose();
        }
    }

}
