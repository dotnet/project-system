// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel
{
    internal sealed class ToolWindowViewModel
    {
        private BuildTreeViewModel _currentBuildItem;

        public ObservableCollection<BuildTreeViewModel> BuildItems { get; }

        public ToolWindowViewModel()
        {
            BuildItems = new ObservableCollection<BuildTreeViewModel>();
        }

        public void NotifyBuildOperationEnded(BuildOperation e)
        {
            _currentBuildItem?.Completed();
            _currentBuildItem = null;
        }

        public void NotifyBuildOperationStarted(BuildOperation buildOperation)
        {
            _currentBuildItem = new BuildTreeViewModel(buildOperation);
            BuildItems.Add(_currentBuildItem);
        }

        public void NotifyBuildStarted(ConfiguredProject configuredProject)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_currentBuildItem == null)
                {
                    _currentBuildItem = new BuildTreeViewModel(BuildOperation.DesignTime);
                    BuildItems.Add(_currentBuildItem);
                }
                _currentBuildItem.Children.Add(new LogTreeViewModel(configuredProject));
            });
        }

        public void NotifyBuildEnded(ConfiguredProject configuredProject)
        {
        }
    }
}
