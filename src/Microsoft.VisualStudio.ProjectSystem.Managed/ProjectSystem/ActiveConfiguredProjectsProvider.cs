// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IActiveConfiguredProjectsProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
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
                        var targetFramework = configuration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty];
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

        public async Task<ImmutableArray<ConfiguredProject>> GetActiveConfiguredProjectsAsync()
        {
            var projectMap = await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);
            return projectMap.Values.ToImmutableArray();
        }

        public async Task<ImmutableArray<ProjectConfiguration>> GetActiveProjectConfigurationsAsync()
        {
            var projectMap = await GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);
            return projectMap.Values.Select(p => p.ProjectConfiguration).ToImmutableArray();
        }
    }
}
