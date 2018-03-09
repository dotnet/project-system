// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class RootViewModel
    {
        public ObservableCollection<LogViewModel> Children { get; }

        public RootViewModel(ObservableCollection<LogViewModel> logs)
        {
            Children = logs;
        }
    }
}
