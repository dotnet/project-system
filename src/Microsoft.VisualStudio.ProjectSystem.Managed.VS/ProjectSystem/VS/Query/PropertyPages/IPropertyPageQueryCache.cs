// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Caches data that we expect to access frequently while processing queries for property page information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface exists in order to simplify unit tests of the data produers.
    /// </para>
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
    internal interface IPropertyPageQueryCache
    {
        /// <summary>
        /// Binds the specified schema to a particular context within the given project configuration.
        /// </summary>
        Task<IRule?> BindToRule(ProjectConfiguration projectConfiguration, string schemaName, QueryProjectPropertiesContext context);
        Task<IImmutableSet<ProjectConfiguration>?> GetKnownConfigurationsAsync();
        Task<ProjectConfiguration?> GetSuggestedConfigurationAsync();

        (string versionKey, long versionNumber) GetUnconfiguredProjectVersion();
        Task<(string versionKey, long versionNumber)> GetConfiguredProjectVersionAsync(ProjectConfiguration configuration);
    }
}
