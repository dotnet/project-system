// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Export(typeof(IBuildLogger))]
    internal sealed class BuildLogger : IBuildLogger
    {
        private readonly ToolWindowViewModel _toolWindow = new ToolWindowViewModel();

        public bool IsLogging { get; private set; }

        public ObservableCollection<BuildTreeViewModel> BuildItems => _toolWindow.BuildItems;

        public void Start() => IsLogging = true;

        public void Stop() => IsLogging = false;

        public void OnBuildStarted(BuildOperation operation) => _toolWindow.OnBuildStarted(operation);

        public void OnBuildEnded(BuildOperation operation) => _toolWindow.OnBuildEnded(operation);
    }
}
