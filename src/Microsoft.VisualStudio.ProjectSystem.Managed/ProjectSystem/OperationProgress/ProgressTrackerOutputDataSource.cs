// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.OperationProgress
{
    internal class ProgressTrackerOutputDataSource : IProgressTrackerOutputDataSource
    {
        public ProgressTrackerOutputDataSource(object owner, ConfiguredProject configuredProject, string operationProgressStageId, string name, string displayMessage)
        {
            Owner = owner;
            ConfiguredProject = configuredProject;
            OperationProgressStageId = operationProgressStageId;
            Name = name;
            DisplayMessage = displayMessage;
        }

        // For inspection of dumps
        public object Owner { get; }

        public ConfiguredProject ConfiguredProject { get; }

        public string OperationProgressStageId { get; }

        public string Name { get; }

        public string DisplayMessage { get; }
    }
}
