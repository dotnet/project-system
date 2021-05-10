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
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;

        public LaunchProfileByIdDataProducer(ILaunchProfilePropertiesAvailableStatus properties, IProjectService2 projectService, IPropertyPageQueryCacheProvider queryCacheProvider)
        {
            _properties = properties;
            _projectService = projectService;
            _queryCacheProvider = queryCacheProvider;
        }

        protected override Task<IEntityValue?> TryCreateEntityOrNullAsync(IQueryExecutionContext executionContext, EntityIdentity id)
        {
            if (QueryProjectPropertiesContext.TryCreateFromEntityId(id, out QueryProjectPropertiesContext? context)
                && StringComparers.ItemTypes.Equals(context.ItemType, "LaunchProfile"))
            {
                return CreateLaunchProfileValueAsync(executionContext, id, context);
            }

            return NullEntityValue;
        }

        private async Task<IEntityValue?> CreateLaunchProfileValueAsync(IQueryExecutionContext executionContext, EntityIdentity id, QueryProjectPropertiesContext context)
        {
            if (_projectService.GetLoadedProject(context.File) is UnconfiguredProject project
                && project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsProvider>() is ILaunchSettingsProvider launchSettingsProvider
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && await launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite) is ILaunchSettings launchSettings)
            {
                foreach ((int index, ProjectSystem.Debug.ILaunchProfile profile) in launchSettings.Profiles.WithIndices())
                {
                    if (StringComparers.LaunchProfileNames.Equals(profile.Name, context.ItemName)
                        && !Strings.IsNullOrEmpty(profile.CommandName))
                    {
                        foreach (Rule rule in DebugUtilities.GetDebugChildRules(projectCatalog))
                        {
                            if (rule.Metadata.TryGetValue("CommandName", out object? commandNameObj)
                                && commandNameObj is string commandName
                                && StringComparers.LaunchProfileCommandNames.Equals(commandName, profile.CommandName))
                            {
                                IPropertyPageQueryCache? queryCache = _queryCacheProvider.CreateCache(project);

                                IEntityValue launchProfileValue = LaunchProfileDataProducer.CreateLaunchProfileValue(
                                    executionContext,
                                    id,
                                    context,
                                    rule,
                                    index,
                                    queryCache,
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
