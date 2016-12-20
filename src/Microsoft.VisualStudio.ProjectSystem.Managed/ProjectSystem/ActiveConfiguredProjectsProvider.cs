// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;

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
    [Export(typeof(ActiveConfiguredProjectsProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ActiveConfiguredProjectsProvider
    {
        private readonly IUnconfiguredProjectServices _services;
        private readonly IUnconfiguredProjectCommonServices _commonServices;

        [ImportingConstructor]
        public ActiveConfiguredProjectsProvider(IUnconfiguredProjectServices services, IUnconfiguredProjectCommonServices commonServices)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(commonServices, nameof(commonServices));
            
            _services = services;
            _commonServices = commonServices;
        }

        /// <summary>
        /// Gets all the active configured projects by TargetFramework dimension for the current unconfigured project.
        /// If the current project is not a cross-targeting project, then it returns a singleton key-value pair with an ignorable key and single active configured project as value.
        /// </summary>
        /// <returns>Map from TargetFramework dimension to active configured project.</returns>
        public async Task<ImmutableDictionary<string, ConfiguredProject>> GetActiveConfiguredProjectsMapAsync()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ConfiguredProject>();
            var knownConfigurations = await _services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync().ConfigureAwait(true);
            var isCrossTarging = knownConfigurations.All(c => c.IsCrossTargeting());
            if (isCrossTarging)
            {
                // Get all the project configurations with all dimensions (ignoring the TargetFramework) matching the active configuration.
                var activeConfiguration = _services.ActiveConfiguredProjectProvider.ActiveProjectConfiguration;
                foreach (var configuration in knownConfigurations)
                {
                    if (configuration.EqualIgnoringTargetFramework(activeConfiguration))
                    {
                        var configuredProject = await _commonServices.Project.LoadConfiguredProjectAsync(configuration).ConfigureAwait(true);
                        var targetFramework = configuration.Dimensions[TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName];
                        builder.Add(targetFramework, configuredProject);
                    }
                }
            }
            else
            {
                builder.Add(string.Empty, _services.ActiveConfiguredProjectProvider.ActiveConfiguredProject);
            }

            return builder.ToImmutableDictionary();
        }

        /// <summary>
        /// Gets all the active configured projects for the current unconfigured project.
        /// </summary>
        /// <returns>Set of active configured projects.</returns>
        public async Task<ImmutableArray<ConfiguredProject>> GetActiveConfiguredProjectsAsync()
        {
            var projectMap = await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);
            return projectMap.Values.ToImmutableArray();
        }

        /// <summary>
        /// Gets all the active project configurations for the current unconfigured project.
        /// </summary>
        /// <returns>Set of active project configurations.</returns>
        public async Task<ImmutableArray<ProjectConfiguration>> GetActiveProjectConfigurationsAsync()
        {
            var projectMap = await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);
            return projectMap.Values.Select(p => p.ProjectConfiguration).ToImmutableArray();
        }
    }
}
