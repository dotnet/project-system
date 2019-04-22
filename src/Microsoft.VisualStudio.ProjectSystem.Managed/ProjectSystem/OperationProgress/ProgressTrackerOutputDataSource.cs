// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public ConfiguredProject ConfiguredProject
        {
            get;
        }

        public string OperationProgressStageId
        {
            get;
        }

        public string Name
        {
            get;
        }

        public string DisplayMessage
        {
            get;
        }
    }
}
