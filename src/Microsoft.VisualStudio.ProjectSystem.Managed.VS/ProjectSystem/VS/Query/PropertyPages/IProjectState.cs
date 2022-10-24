// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Holds information about the project that is generally needed by Project Query API data providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The expectation is that at most one instance of this type will be created per query, and that instance will be
    /// passed from one provider to the next as part of <see cref="IEntityValueFromProvider.ProviderState"/>. This
    /// allows us to maximize the use of cached data within a query, but we are also guaranteed that the cache will not be
    /// held past the end of the query.
    /// </para>
    /// <para>
    /// As an example, consider what needs to occur when we want to retrieve the set of <see cref="IUIPropertyValueSnapshot"/>s
    /// for a <see cref="IUIPropertySnapshot"/>:
    /// <list type="bullet">
    /// <item>Retrieve the set of known configurations from the <see cref="UnconfiguredProject"/>.</item>
    /// <item>For each configuration, get the property page catalog.</item>
    /// <item>Retrieve the <see cref="IRule"/> for the property page from the catalog.</item>
    /// <item>Find the property within the <see cref="IRule"/>.</item>
    /// <item>Retrieve the property value.</item>
    /// </list>
    /// The <see cref="UIPropertyValueDataProvider"/> produces <see cref="IUIPropertyValueSnapshot"/>s one at a time, and needs
    /// to do these steps for each one. Given that a query will likely retrieve values for multiple properties on
    /// multiple pages across multiple configurations, introducing caching at key levels in the process can
    /// significantly reduce the amount of work we need to do.
    /// </para>
    /// </remarks>
    internal interface IProjectState
    {
        /// <summary>
        /// Binds the specified schema to a particular context within the given project configuration.
        /// </summary>
        Task<IRule?> BindToRuleAsync(ProjectConfiguration projectConfiguration, string schemaName, QueryProjectPropertiesContext propertiesContext);
        Task<IImmutableSet<ProjectConfiguration>?> GetKnownConfigurationsAsync();
        Task<ProjectConfiguration?> GetSuggestedConfigurationAsync();
        Task<(string versionKey, long versionNumber)?> GetMetadataVersionAsync();
        Task<(string versionKey, long versionNumber)?> GetDataVersionAsync(ProjectConfiguration configuration);
    }
}
