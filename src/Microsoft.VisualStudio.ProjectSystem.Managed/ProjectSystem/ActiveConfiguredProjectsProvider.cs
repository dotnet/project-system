// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IActiveConfiguredProjectsProvider))]
    internal class ActiveConfiguredProjectsProvider : IActiveConfiguredProjectsProvider
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

        public async Task<ImmutableDictionary<string, ConfiguredProject>> GetActiveConfiguredProjectsMapAsync()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ConfiguredProject>();

            ImmutableArray<ConfiguredProject> projects = await GetActiveConfiguredProjectsAsync().ConfigureAwait(false);

            var isCrossTargeting = projects.All(project => project.ProjectConfiguration.IsCrossTargeting());
            if (isCrossTargeting)
            {
                foreach (ConfiguredProject project in projects)
                {
                    var targetFramework = project.ProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty];
                    builder.Add(targetFramework, project);
                }
            }
            else
            {
                builder.Add(string.Empty, projects.First());
            }

            return builder.ToImmutable();
        }

        public async Task<ImmutableArray<ConfiguredProject>> GetActiveConfiguredProjectsAsync()
        {
            var builder = ImmutableArray.CreateBuilder<ConfiguredProject>();

            ImmutableArray<ProjectConfiguration> configurations = await GetActiveProjectConfigurationsAsync().ConfigureAwait(false);
            foreach (ProjectConfiguration configuration in configurations)
            {
                var project = await _commonServices.Project.LoadConfiguredProjectAsync(configuration)
                                                           .ConfigureAwait(false);

                builder.Add(project);
            }

            return builder.ToImmutable();
        }

        public async Task<ImmutableArray<ProjectConfiguration>> GetActiveProjectConfigurationsAsync()
        {
            ProjectConfiguration activeConfiguration = _services.ActiveConfiguredProjectProvider.ActiveProjectConfiguration;
            if (activeConfiguration == null)
                return ImmutableArray<ProjectConfiguration>.Empty;

            var builder = ImmutableArray.CreateBuilder<ProjectConfiguration>();

            IImmutableSet<ProjectConfiguration> configurations = await _services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync()
                                                                                                             .ConfigureAwait(false);

            foreach (ProjectConfiguration configuration in configurations)
            {
                if (IsActiveConfigurationCandidate(activeConfiguration, configuration))
                { 
                    builder.Add(configuration);
                }
            }

            Assumes.True(builder.Count > 0, "We have an active configuration that isn't one of the known configurations");
            return builder.ToImmutable();
        }

        private static bool IsActiveConfigurationCandidate(ProjectConfiguration activeConfiguration, ProjectConfiguration configuration)
        {
            return configuration.EqualIgnoringTargetFramework(activeConfiguration);
        }
    }
}
