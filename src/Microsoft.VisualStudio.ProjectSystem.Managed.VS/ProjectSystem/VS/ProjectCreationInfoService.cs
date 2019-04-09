// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectCreationInfoService))]
    internal class ProjectCreationInfoService : IProjectCreationInfoService
    {
        public bool IsNewlyCreated(UnconfiguredProject project)
        {
            IProjectCreationState projectCreationState = project.Services.ExportProvider.GetExportedValueOrDefault<IProjectCreationState>();
            return projectCreationState?.WasNewlyCreated ?? false;
        }
    }
}
