// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides set of configured projects whose ProjectConfiguration has all dimensions (except the TargetFramework) matching the active VS configuration.
    ///
    ///     For example, for a cross-targeting project with TargetFrameworks = "net45;net46" we have:
    ///     -> All known configurations:
    ///         Debug | AnyCPU | net45
    ///         Debug | AnyCPU | net46
    ///         Release | AnyCPU | net45
    ///         Release | AnyCPU | net46
    ///         
    ///     -> Say, active VS configuration = "Debug | AnyCPU"
    ///       
    ///     -> Active configurations returned by this provider:
    ///         Debug | AnyCPU | net45
    ///         Debug | AnyCPU | net46
    /// </summary>
    internal interface IActiveConfiguredProjectsProvider
    {
        /// <summary>
        /// Gets all the active configured projects by TargetFramework dimension for the current unconfigured project.
        /// If the current project is not a cross-targeting project, then it returns a singleton key-value pair with an ignorable key and single active configured project as value.
        /// </summary>
        /// <returns>Map from TargetFramework dimension to active configured project.</returns>
        Task<ImmutableDictionary<string, ConfiguredProject>> GetActiveConfiguredProjectsMapAsync();

        /// <summary>
        ///     Returns the ordered list of configured projects that are active for the current project, loading them if needed.
        /// </summary>
        /// <returns>
        ///     An <see cref="ActiveConfiguredObjects{T}"/> containing the ordered set of <see cref="ConfiguredProject"/> objects 
        ///     with the names of the configuration dimensions that participated in the calculation of the active 
        ///     <see cref="ConfiguredProject"/> objects, or <see langword="null"/> if there are no active <see cref="ConfiguredProject"/>
        ///     objects.
        /// </returns>
        /// <remarks>
        ///     The order in the returned <see cref="ActiveConfiguredObjects{T}"/> matches the declared ordered within
        ///     the project file.
        /// </remarks>
        Task<ActiveConfiguredObjects<ConfiguredProject>> GetActiveConfiguredProjectsAsync();

        /// <summary>
        ///     Returns the ordered list of project configurations that are active for the current project.
        /// </summary>
        /// <returns>
        ///     An <see cref="ActiveConfiguredObjects{T}"/> containing the ordered set of <see cref="ProjectConfiguration"/> objects 
        ///     with the names of the configuration dimensions that participated in the calculation of the active 
        ///     <see cref="ProjectConfiguration"/> objects, or <see langword="null"/> if there are no active 
        ///     <see cref="ProjectConfiguration"/> objects.
        /// </returns>
        /// <remarks>
        ///     The order in the returned <see cref="ActiveConfiguredObjects{T}"/> matches the declared ordered within
        ///     the project file.
        /// </remarks>
        Task<ActiveConfiguredObjects<ProjectConfiguration>> GetActiveProjectConfigurationsAsync();
    }
}
