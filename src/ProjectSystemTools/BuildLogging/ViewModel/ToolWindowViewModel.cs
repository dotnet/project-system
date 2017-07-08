// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel
{
    internal sealed class ToolWindowViewModel
    {
        private BuildTreeViewModel _currentBuildItem;
        private readonly Dictionary<IBuild, LogTreeViewModel> _currentLogs = new Dictionary<IBuild, LogTreeViewModel>();

        public ObservableCollection<BuildTreeViewModel> BuildItems { get; }

        public ToolWindowViewModel()
        {
            BuildItems = new ObservableCollection<BuildTreeViewModel>();
        }

        public void Clear()
        {
            _currentBuildItem = null;
            _currentLogs.Clear();
            BuildItems.Clear();
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

        public void NotifyBuildStarted(IBuild build)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_currentBuildItem == null)
                {
                    _currentBuildItem = new BuildTreeViewModel(BuildOperation.DesignTime);
                    BuildItems.Add(_currentBuildItem);
                }
                var log = new LogTreeViewModel(build.ConfiguredProject, build.Targets);
                _currentLogs[build] = log;
                _currentBuildItem.Children.Add(log);
            });
        }

        public void NotifyBuildEnded(IBuild build)
        {
            if (_currentLogs.TryGetValue(build, out var log))
            {
                log.Completed();
                _currentLogs.Remove(build);
            }
        }
    }
}
