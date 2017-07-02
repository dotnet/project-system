// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel
{
    internal sealed class ToolWindowViewModel : IDisposable
    {
        private readonly BuildLogger _buildLogger;
        private BuildTreeViewModel _currentBuildItem;

        public ObservableCollection<BuildTreeViewModel> BuildItems { get; }

        public ToolWindowViewModel(BuildLogger buildLogger)
        {
            BuildItems = new ObservableCollection<BuildTreeViewModel>();
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
            _currentBuildItem = new BuildTreeViewModel(buildOperation);
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
