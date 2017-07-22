// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Export(typeof(IBuildManager))]
    internal sealed class BuildManager : IBuildManager
    {
        private readonly ToolWindowViewModel _toolWindow = new ToolWindowViewModel();

        public bool IsLogging { get; private set; }

        public ObservableCollection<BuildTreeViewModel> Builds => _toolWindow.Builds;

        public void Start() => IsLogging = true;

        public void Stop() => IsLogging = false;

        public void Clear()
        {
            _toolWindow.Clear();
        }

        public void NotifyBuildStarted(BuildOperation operation)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyBuildStarted(operation);
            }
        }

        public void NotifyBuildEnded(BuildOperation operation)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyBuildEnded(operation);
            }
        }

        public void NotifyProjectStarted(ProjectStartedEventArgs project)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyProjectStarted(project);
            }
        }

        public void NotifyProjectEnded(ProjectFinishedEventArgs project)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyProjectEnded(project);
            }
        }
    }
}
