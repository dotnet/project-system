// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.OperationProgress
{
    internal class ProgressTrackerOutputDataSource : IProgressTrackerOutputDataSource
    {
        public ProgressTrackerOutputDataSource(ConfiguredProject configuredProject, string operationProgressStageId, string name, string displayMessage)
        {
            ConfiguredProject = configuredProject;
            OperationProgressStageId = operationProgressStageId;
            Name = name;
            DisplayMessage = displayMessage;
        }

        public ConfiguredProject ConfiguredProject { get; }

        public string OperationProgressStageId { get; }

        public string Name { get; }

        public string DisplayMessage { get; }
    }
}
