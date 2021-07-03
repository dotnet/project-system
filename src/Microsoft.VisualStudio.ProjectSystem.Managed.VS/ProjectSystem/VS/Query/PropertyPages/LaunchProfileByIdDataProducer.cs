// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

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
            if (QueryProjectPropertiesContext.TryCreateFromEntityId(id, out QueryProjectPropertiesContext? propertiesContext)
                && StringComparers.ItemTypes.Equals(propertiesContext.ItemType, "LaunchProfile"))
            {
                return CreateLaunchProfileValueAsync(queryExecutionContext, id, propertiesContext);
            }

            return NullEntityValue;
        }

        private async Task<IEntityValue?> CreateLaunchProfileValueAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id, QueryProjectPropertiesContext propertiesContext)
        {
            if (_projectService.GetLoadedProject(propertiesContext.File) is UnconfiguredProject project
                && project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsProvider>() is ILaunchSettingsProvider launchSettingsProvider
                && project.Services.ExportProvider.GetExportedValueOrDefault<LaunchSettingsTracker>() is LaunchSettingsTracker launchSettingsTracker
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && await launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite) is ILaunchSettings launchSettings)
            {
                if (launchSettings is IVersionedLaunchSettings versionedLaunchSettings)
                {
                    queryExecutionContext.ReportInputDataVersion(launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);
                }

                IProjectState projectState = new LaunchProfileProjectState(project, launchSettingsProvider, launchSettingsTracker);

                foreach ((int index, ProjectSystem.Debug.ILaunchProfile profile) in launchSettings.Profiles.WithIndices())
                {
                    if (StringComparers.LaunchProfileNames.Equals(profile.Name, propertiesContext.ItemName)
                        && !Strings.IsNullOrEmpty(profile.CommandName))
                    {
                        foreach (Rule rule in DebugUtilities.GetDebugChildRules(projectCatalog))
                        {
                            if (rule.Metadata.TryGetValue("CommandName", out object? commandNameObj)
                                && commandNameObj is string commandName
                                && StringComparers.LaunchProfileCommandNames.Equals(commandName, profile.CommandName))
                            {
                                IEntityValue launchProfileValue = LaunchProfileDataProducer.CreateLaunchProfileValue(
                                    queryExecutionContext,
                                    id,
                                    propertiesContext,
                                    rule,
                                    index,
                                    projectState,
                                    _properties);
                                return launchProfileValue;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
