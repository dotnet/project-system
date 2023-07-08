// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal class LaunchProfileByIdDataProducer : QueryDataByIdProducerBase
    {
        private readonly ILaunchProfilePropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public LaunchProfileByIdDataProducer(ILaunchProfilePropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            _properties = properties;
            _projectService = projectService;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id)
        {
            string projectPath = ValidateIdAndExtractProjectPath(id);

            if (_projectService.GetLoadedProject(projectPath) is UnconfiguredProject project
                && project.Services.ExportProvider.GetExportedValueOrDefault<IProjectLaunchProfileHandler>() is IProjectLaunchProfileHandler launchProfileHandler)
            {
                return launchProfileHandler.RetrieveLaunchProfileEntityAsync(queryExecutionContext, id, _properties);
            }

            return NullEntityValue;
        }

        private static string ValidateIdAndExtractProjectPath(EntityIdentity id)
        {
            Assumes.True(id.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string? projectPath));
            return projectPath;
        }
    }
}
