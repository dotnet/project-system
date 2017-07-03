// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Export(typeof(IBuildManager))]
    internal sealed class BuildManager : IBuildManager
    {
        private readonly ToolWindowViewModel _toolWindow = new ToolWindowViewModel();

        public bool IsLogging { get; private set; }

        public ObservableCollection<BuildTreeViewModel> BuildItems => _toolWindow.BuildItems;

        public void Start() => IsLogging = true;

        public void Stop() => IsLogging = false;

        public void NotifyBuildOperationStarted(BuildOperation operation)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyBuildOperationStarted(operation);
            }
        }

        public void NotifyBuildOperationEnded(BuildOperation operation)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyBuildOperationEnded(operation);
            }
        }

        public void NotifyBuildStarted(ConfiguredProject configuredProject)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyBuildStarted(configuredProject);
            }
        }

        public void NotifyBuildEnded(ConfiguredProject configuredProject)
        {
            if (IsLogging)
            {
                _toolWindow.NotifyBuildEnded(configuredProject);
            }
        }
    }
}
