// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal interface IBuildLogger
    {
        bool IsLogging { get; }

        ObservableCollection<BuildTreeViewModel> BuildItems { get; }

        void Start();

        void Stop();

        void OnBuildStarted(BuildOperation operation);

        void OnBuildEnded(BuildOperation operation);
    }
}