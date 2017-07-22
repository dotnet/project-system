// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal interface IBuildManager
    {
        bool IsLogging { get; }

        ObservableCollection<BuildTreeViewModel> Builds { get; }

        void Start();

        void Stop();

        void Clear();

        void NotifyBuildStarted(BuildOperation operation);

        void NotifyBuildEnded(BuildOperation operation);

        void NotifyProjectStarted(ProjectStartedEventArgs project);

        void NotifyProjectEnded(ProjectFinishedEventArgs project);
    }
}