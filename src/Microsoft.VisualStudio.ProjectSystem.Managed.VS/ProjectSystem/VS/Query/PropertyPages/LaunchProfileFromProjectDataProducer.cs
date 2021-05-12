// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal class LaunchProfileFromProjectDataProducer : QueryDataFromProviderStateProducerBase<UnconfiguredProject>
    {
        private readonly ILaunchProfilePropertiesAvailableStatus _properties;
        private readonly IProjectStateProvider _queryCacheProvider;

        public LaunchProfileFromProjectDataProducer(ILaunchProfilePropertiesAvailableStatus properties, IProjectStateProvider queryCacheProvider)
        {
            _properties = properties;
            _queryCacheProvider = queryCacheProvider;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, UnconfiguredProject providerState)
        {
            return CreateLaunchProfileValuesAsync(queryExecutionContext, parent, providerState);
        }

        private async Task<IEnumerable<IEntityValue>> CreateLaunchProfileValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, UnconfiguredProject project)
        {
            if (project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsProvider>() is ILaunchSettingsProvider launchSettingsProvider
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && await launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite) is ILaunchSettings launchSettings)
            {
                return createLaunchProfileValues();
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createLaunchProfileValues()
            {
                IProjectState propertyPageQueryCache = _queryCacheProvider.CreateState(project);

                Dictionary<string, Rule> debugRules = new();
                foreach (Rule rule in DebugUtilities.GetDebugChildRules(projectCatalog))
                {
                    if (rule.Metadata.TryGetValue("CommandName", out object? commandNameObj)
                        && commandNameObj is string commandName)
                    {
                        debugRules[commandName] = rule;
                    }
                }

                foreach ((int index, ProjectSystem.Debug.ILaunchProfile profile) in launchSettings.Profiles.WithIndices())
                {
                    if (!Strings.IsNullOrEmpty(profile.Name)
                        && !Strings.IsNullOrEmpty(profile.CommandName)
                        && debugRules.TryGetValue(profile.CommandName, out Rule rule))
                    {
                        QueryProjectPropertiesContext propertiesContext = new(
                            isProjectFile: true,
                            file: project.FullPath,
                            itemType: LaunchProfileProjectItemProvider.ItemType,
                            itemName: profile.Name);

                        IEntityValue launchProfileValue = CreateLaunchProfileValue(queryExecutionContext, parent, propertiesContext, rule, index, propertyPageQueryCache);
                        yield return launchProfileValue;
                    }
                }
            }
        }

        private IEntityValue CreateLaunchProfileValue(IQueryExecutionContext queryExecutionContext, IEntityValue parent, QueryProjectPropertiesContext propertiesContext, Rule rule, int order, IProjectState propertyPageQueryCache)
        {
            EntityIdentity identity = new(
                ((IEntityWithId)parent).Id,
                new Dictionary<string, string>
                {
                    { ProjectModelIdentityKeys.SourceItemType, propertiesContext.ItemType! },
                    { ProjectModelIdentityKeys.SourceItemName, propertiesContext.ItemName! }
                });

            return LaunchProfileDataProducer.CreateLaunchProfileValue(queryExecutionContext, identity, propertiesContext, rule, order, propertyPageQueryCache, _properties);
        }
    }
}
