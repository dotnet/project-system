// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal class PropertyPageQueryCache : IPropertyPageQueryCache
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly Dictionary<ProjectConfiguration, IPropertyPagesCatalog?> _catalogCache;
        private readonly Dictionary<(ProjectConfiguration, string, QueryProjectPropertiesContext), IRule?> _ruleCache;

        private readonly AsyncLazy<IImmutableSet<ProjectConfiguration>?> _knownProjectConfigurations;
        private readonly AsyncLazy<ProjectConfiguration?> _defaultProjectConfiguration;

        public PropertyPageQueryCache(UnconfiguredProject project)
        {
            _unconfiguredProject = project;
            JoinableTaskFactory joinableTaskFactory = project.Services.ThreadingPolicy.JoinableTaskFactory;

            _knownProjectConfigurations = new AsyncLazy<IImmutableSet<ProjectConfiguration>?>(CreateKnownConfigurationsAsync, joinableTaskFactory);
            _defaultProjectConfiguration = new AsyncLazy<ProjectConfiguration?>(CreateDefaultConfigurationAsync, joinableTaskFactory);
            _catalogCache = new Dictionary<ProjectConfiguration, IPropertyPagesCatalog?>();
            _ruleCache = new Dictionary<(ProjectConfiguration, string, QueryProjectPropertiesContext), IRule?>();
        }

        /// <summary>
        /// Retrieves the set of <see cref="ProjectConfiguration"/>s for the project.
        /// Use this when you actually need all of the <see cref="ProjectConfiguration"/>s;
        /// use <see cref="GetSuggestedConfigurationAsync"/> when you just need any
        /// <see cref="ProjectConfiguration"/>.
        /// </summary>
        public Task<IImmutableSet<ProjectConfiguration>?> GetKnownConfigurationsAsync() => _knownProjectConfigurations.GetValueAsync();

        /// <summary>
        /// Retrieves a default <see cref="ProjectConfiguration"/> for the project.
        /// </summary>
        public Task<ProjectConfiguration?> GetSuggestedConfigurationAsync() => _defaultProjectConfiguration.GetValueAsync();

        private async Task<IImmutableSet<ProjectConfiguration>?> CreateKnownConfigurationsAsync()
        {
            if (_unconfiguredProject.Services.ProjectConfigurationsService is IProjectConfigurationsService configurationsService)
            {
                return await configurationsService.GetKnownProjectConfigurationsAsync();
            }

            return null;
        }

        private async Task<ProjectConfiguration?> CreateDefaultConfigurationAsync()
        {
            if (_unconfiguredProject.Services.ProjectConfigurationsService is IProjectConfigurationsService2 configurationsService2)
            {
                return await configurationsService2.GetSuggestedProjectConfigurationAsync();
            }
            else if (_unconfiguredProject.Services.ProjectConfigurationsService is IProjectConfigurationsService configurationsService)
            {
                return configurationsService.SuggestedProjectConfiguration;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves the set of property pages that apply to the project level for the given <paramref
        /// name="projectConfiguration"/>.
        /// </summary>
        private async Task<IPropertyPagesCatalog?> GetProjectLevelPropertyPagesCatalogAsync(ProjectConfiguration projectConfiguration)
        {
            if (_catalogCache.TryGetValue(projectConfiguration, out IPropertyPagesCatalog? cachedCatalog))
            {
                return cachedCatalog;
            }

            ConfiguredProject configuredProject = await _unconfiguredProject.LoadConfiguredProjectAsync(projectConfiguration);
            IPropertyPagesCatalog? catalog = await configuredProject.GetProjectLevelPropertyPagesCatalogAsync();

            _catalogCache.Add(projectConfiguration, catalog);
            return catalog;
        }

        /// <summary>
        /// Retrieves the <see cref="IRule"/> with name "<paramref name="schemaName"/>" within the given <paramref
        /// name="projectConfiguration"/> and <paramref name="context"/>.
        /// </summary>
        public async Task<IRule?> BindToRule(ProjectConfiguration projectConfiguration, string schemaName, QueryProjectPropertiesContext context)
        {
            if (_ruleCache.TryGetValue((projectConfiguration, schemaName, context), out IRule? cachedRule))
            {
                return cachedRule;
            }

            IRule? rule = null;
            if (await GetProjectLevelPropertyPagesCatalogAsync(projectConfiguration) is IPropertyPagesCatalog catalog)
            {
                rule = catalog.BindToContext(schemaName, context);
            }

            _ruleCache.Add((projectConfiguration, schemaName, context), rule);
            return rule;
        }

        public (string versionKey, long versionNumber) GetUnconfiguredProjectVersion()
        {
            _unconfiguredProject.GetQueryDataVersion(out string versionKey, out long versionNumber);
            return (versionKey, versionNumber);
        }

        public async Task<(string versionKey, long versionNumber)> GetConfiguredProjectVersionAsync(ProjectConfiguration configuration)
        {
            ConfiguredProject configuredProject = await _unconfiguredProject.LoadConfiguredProjectAsync(configuration);
            configuredProject.GetQueryDataVersion(out string versionKey, out long versionNumber);
            return (versionKey, versionNumber);
        }
    }
}
