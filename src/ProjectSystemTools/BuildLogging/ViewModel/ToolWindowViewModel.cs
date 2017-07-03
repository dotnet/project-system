// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public void OnBuildEnded(BuildOperation e)
        {
            _currentBuildItem?.Completed();
            _currentBuildItem = null;
        }

        public void OnBuildStarted(BuildOperation buildOperation)
        {
            _currentBuildItem = new BuildTreeViewModel(buildOperation);
            BuildItems.Add(_currentBuildItem);
        }
    }

}
