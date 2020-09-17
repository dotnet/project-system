// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Caches data that we expect to access frequently while processing queries for property page information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The expectation is that at most one instance of this type will be created per query, and that instance will be
    /// passed from one provider to the next as part of <see cref="IEntityValueFromProvider.ProviderState"/>. This
    /// allows us to maximize the use of the cache within a query, but we are also guaranteed that the cache will not be
    /// held past the end of the query.
    /// </para>
    /// <para>
    /// As an example, consider what needs to occur when we want to retrieve the set of <see cref="IUIPropertyValue"/>s
    /// for a <see cref="IUIProperty"/>:
    /// <list type="bullet">
    /// <item>Retrieve the set of known configurations from the <see cref="UnconfiguredProject"/>.</item>
    /// <item>For each configuration, get the property page catalog.</item>
    /// <item>Retrieve the <see cref="IRule"/> for the property page from the catalog.</item>
    /// <item>Find the property within the <see cref="IRule"/>.</item>
    /// <item>Retrieve the property value.</item>
    /// </list>
    /// The <see cref="UIPropertyValueDataProvider"/> produces <see cref="IUIPropertyValue"/>s one at a time, and needs
    /// to do these steps for each one. Given that a query will likely retrieve values for multiple properties on
    /// multiple pages across multiple configurations, introducing caching at key levels in the process can
    /// significantly reduce the amount of work we need to do.
    /// </para>
    /// </remarks>
    internal class PropertyPageQueryCache
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly Dictionary<ProjectConfiguration, IPropertyPagesCatalog?> _catalogCache;
        private readonly Dictionary<(ProjectConfiguration, string), IRule?> _ruleCache;

        private readonly AsyncLazy<IImmutableSet<ProjectConfiguration>?> _knownProjectConfigurations;

        public PropertyPageQueryCache(UnconfiguredProject project)
        {
            _unconfiguredProject = project;
            var joinableTaskFactory = project.Services.ThreadingPolicy.JoinableTaskFactory;

            _knownProjectConfigurations = new AsyncLazy<IImmutableSet<ProjectConfiguration>?>(CreateKnownConfigurationsAsync, joinableTaskFactory);
            _catalogCache = new Dictionary<ProjectConfiguration, IPropertyPagesCatalog?>();
            _ruleCache = new Dictionary<(ProjectConfiguration, string), IRule?>();
        }

        /// <summary>
        /// Retrieves the set of <see cref="ProjectConfiguration"/>s for the project.
        /// </summary>
        public Task<IImmutableSet<ProjectConfiguration>?> GetKnownConfigurationsAsync() => _knownProjectConfigurations.GetValueAsync();

        private async Task<IImmutableSet<ProjectConfiguration>?> CreateKnownConfigurationsAsync()
        {
            if (_unconfiguredProject.Services.ProjectConfigurationsService is IProjectConfigurationsService configurationsService)
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
            if (_catalogCache.TryGetValue(projectConfiguration, out var cachedCatalog))
            {
                return cachedCatalog;
            }

            var configuredProject = await _unconfiguredProject.LoadConfiguredProjectAsync(projectConfiguration);
            var catalog = await configuredProject.GetProjectLevelPropertyPagesCatalogAsync();

            _catalogCache.Add(projectConfiguration, catalog);
            return catalog;
        }

        /// <summary>
        /// Retrieves the <see cref="IRule"/> with name "<paramref name="schemaName"/>" within the given <paramref
        /// name="projectConfiguration"/>.
        /// </summary>
        public async Task<IRule?> BindToRule(ProjectConfiguration projectConfiguration, string schemaName)
        {
            if (_ruleCache.TryGetValue((projectConfiguration, schemaName), out var cachedRule))
            {
                return cachedRule;
            }

            IRule? rule = null;
            if (await GetProjectLevelPropertyPagesCatalogAsync(projectConfiguration) is IPropertyPagesCatalog catalog)
            {
                rule = catalog.BindToContext(schemaName, file: null, itemType: null, itemName: null);
            }

            _ruleCache.Add((projectConfiguration, schemaName), rule);
            return rule;
        }
    }
}
