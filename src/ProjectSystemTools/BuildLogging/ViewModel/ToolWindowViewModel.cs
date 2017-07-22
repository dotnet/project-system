// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel
{
    internal sealed class ToolWindowViewModel
    {
        private BuildTreeViewModel _currentBuild;
        private readonly Dictionary<string, ProjectTreeViewModel> _currentProjects = new Dictionary<string, ProjectTreeViewModel>();

        public ObservableCollection<BuildTreeViewModel> Builds { get; }

        public ToolWindowViewModel()
        {
            Builds = new ObservableCollection<BuildTreeViewModel>();
        }

        public void Clear()
        {
            _currentBuild = null;
            _currentProjects.Clear();
            Builds.Clear();
        }

        public void NotifyBuildStarted(BuildOperation buildOperation)
        {
            _currentBuild = new BuildTreeViewModel(buildOperation);
            Builds.Add(_currentBuild);
        }

        public void NotifyBuildEnded(BuildOperation e)
        {
            _currentBuild?.Completed();
            _currentBuild = null;
        }

        public void NotifyProjectStarted(ProjectStartedEventArgs project)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_currentBuild == null)
                {
                    _currentBuild = new BuildTreeViewModel(BuildOperation.DesignTime);
                    Builds.Add(_currentBuild);
                }
                var log = new ProjectTreeViewModel(
                    project.ProjectFile,
                    project.TargetNames,
                    project.GlobalProperties.TryGetValue("Configuration", out var configuration)
                        ? configuration
                        : string.Empty,
                    project.GlobalProperties.TryGetValue("Platform", out var platform)
                        ? platform
                        : string.Empty,
                    project.Timestamp);
                _currentProjects[project.ProjectFile] = log;
                _currentBuild.Children.Add(log);
            });
        }

        public void NotifyProjectEnded(ProjectFinishedEventArgs project)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_currentProjects.TryGetValue(project.ProjectFile, out var log))
                {
                    log.Completed(project.Timestamp);
                    _currentProjects.Remove(project.ProjectFile);
                }
            });
        }
    }
}
