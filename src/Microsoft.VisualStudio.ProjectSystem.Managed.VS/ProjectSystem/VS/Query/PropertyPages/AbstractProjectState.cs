// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal abstract class AbstractProjectState : IProjectState
    {
        protected readonly UnconfiguredProject Project;

        private readonly Dictionary<ProjectConfiguration, IPropertyPagesCatalog?> _catalogCache;
        private readonly Dictionary<(ProjectConfiguration, string, QueryProjectPropertiesContext), IRule?> _ruleCache;

        private readonly AsyncLazy<IImmutableSet<ProjectConfiguration>?> _knownProjectConfigurations;
        private readonly AsyncLazy<ProjectConfiguration?> _defaultProjectConfiguration;

        protected AbstractProjectState(UnconfiguredProject project)
        {
            Project = project;
            JoinableTaskFactory joinableTaskFactory = project.Services.ThreadingPolicy.JoinableTaskFactory;

            _knownProjectConfigurations = new AsyncLazy<IImmutableSet<ProjectConfiguration>?>(CreateKnownConfigurationsAsync, joinableTaskFactory);
            _defaultProjectConfiguration = new AsyncLazy<ProjectConfiguration?>(CreateDefaultConfigurationAsync, joinableTaskFactory);
            _catalogCache = new Dictionary<ProjectConfiguration, IPropertyPagesCatalog?>();
            _ruleCache = new Dictionary<(ProjectConfiguration, string, QueryProjectPropertiesContext), IRule?>();
        }

        /// <summary>
        /// Retrieves the <see cref="IRule"/> with name "<paramref name="schemaName"/>" within the given <paramref
        /// name="projectConfiguration"/> and <paramref name="propertiesContext"/>.
        /// </summary>
        public async Task<IRule?> BindToRuleAsync(ProjectConfiguration projectConfiguration, string schemaName, QueryProjectPropertiesContext propertiesContext)
        {
            if (_ruleCache.TryGetValue((projectConfiguration, schemaName, propertiesContext), out IRule? cachedRule))
            {
                return cachedRule;
            }

            IRule? rule = null;
            if (await GetProjectLevelPropertyPagesCatalogAsync(projectConfiguration) is IPropertyPagesCatalog catalog)
            {
                rule = catalog.BindToContext(schemaName, propertiesContext);
            }

            _ruleCache.Add((projectConfiguration, schemaName, propertiesContext), rule);
            return rule;
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

        private async Task<ProjectConfiguration?> CreateDefaultConfigurationAsync()
        {
            if (Project.Services.ProjectConfigurationsService is IProjectConfigurationsService2 configurationsService2)
            {
                return await configurationsService2.GetSuggestedProjectConfigurationAsync();
            }
            else if (Project.Services.ProjectConfigurationsService is IProjectConfigurationsService configurationsService)
            {
                return configurationsService.SuggestedProjectConfiguration;
            }
            else
            {
                return null;
            }
        }

        private async Task<IImmutableSet<ProjectConfiguration>?> CreateKnownConfigurationsAsync()
        {
            if (Project.Services.ProjectConfigurationsService is IProjectConfigurationsService configurationsService)
            {
                return await configurationsService.GetKnownProjectConfigurationsAsync();
            }

            return null;
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

            ConfiguredProject configuredProject = await Project.LoadConfiguredProjectAsync(projectConfiguration);
            IPropertyPagesCatalog? catalog = await configuredProject.GetProjectLevelPropertyPagesCatalogAsync();

            _catalogCache.Add(projectConfiguration, catalog);
            return catalog;
        }

        public async Task<(string versionKey, long versionNumber)?> GetMetadataVersionAsync()
        {
            if (await GetSuggestedConfigurationAsync() is ProjectConfiguration configuration)
            {
                ConfiguredProject configuredProject = await Project.LoadConfiguredProjectAsync(configuration);
                configuredProject.GetQueryDataVersion(out string versionKey, out long versionNumber);
                return (versionKey, versionNumber);
            }

            return null;
        }

        public abstract Task<(string versionKey, long versionNumber)?> GetDataVersionAsync(ProjectConfiguration configuration);
    }
}
