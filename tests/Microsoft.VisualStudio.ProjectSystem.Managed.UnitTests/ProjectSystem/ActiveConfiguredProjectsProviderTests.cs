// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Configuration;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class ActiveConfiguredProjectsProviderTests
    {
        [Fact]
        public async Task GetActiveProjectConfigurationsAsync_WhenNoActiveConfiguration_ReturnsNull()
        {
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => null);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider);

            var provider = CreateInstance(services: services);

            var result = await provider.GetActiveProjectConfigurationsAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetActiveConfiguredProjectAsync_WhenNoActiveConfiguration_ReturnsNull()
        {
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => null);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider);

            var provider = CreateInstance(services: services);

            var result = await provider.GetActiveConfiguredProjectsAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetActiveProjectConfigurationsAsync_WhenNoDimensionProviders_ReturnsNoDimensionNames()
        {
            var provider = CreateInstance("Debug|AnyCPU", "Debug|AnyCPU");

            var result = await provider.GetActiveProjectConfigurationsAsync();

            Assert.Empty(result!.DimensionNames);
        }

        [Fact]
        public async Task GetActiveConfiguredProjectAsync_WhenNoDimensionProviders_ReturnsNoDimensionNames()
        {
            var provider = CreateInstance("Debug|AnyCPU", "Debug|AnyCPU");

            var result = await provider.GetActiveConfiguredProjectsAsync();

            Assert.Empty(result!.DimensionNames);
        }

        [Theory] // ActiveConfiguration                 Configurations
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU")]
        [InlineData("Debug|AnyCPU|net46",               "Debug|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU|net46",               "Debug|AnyCPU|net46;Release|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU;Release|AnyCPU")]
        [InlineData("Debug|AnyCPU",                     "Release|AnyCPU;Debug|AnyCPU")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU;Release|AnyCPU;Debug|x86")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU;Release|AnyCPU;Debug|x86;Release|x86")]
        [InlineData("Debug|AnyCPU",                     "Release|AnyCPU;Debug|x86;Release|x86;Debug|AnyCPU")]
        [InlineData("Release|AnyCPU",                   "Debug|AnyCPU;Release|AnyCPU")]
        [InlineData("Release|AnyCPU",                   "Release|AnyCPU;Debug|AnyCPU")]
        [InlineData("Debug|x86",                        "Debug|AnyCPU;Release|AnyCPU;Debug|x86")]
        [InlineData("Release|x86",                      "Debug|AnyCPU;Release|AnyCPU;Debug|x86;Release|x86")]
        [InlineData("Release|x86",                      "Release|AnyCPU;Debug|x86;Release|x86;Debug|AnyCPU")]
        public async Task GetActiveProjectConfigurationsAsync_WhenNoDimensionProviders_ReturnsActiveProjectConfiguration(string activeConfiguration, string configurations)
        {
            var provider = CreateInstance(activeConfiguration, configurations);

            var result = await provider.GetActiveProjectConfigurationsAsync();

            Assert.Single(result!.Objects);
            Assert.Equal(activeConfiguration, result.Objects[0].Name);
        }

        [Theory] // ActiveConfiguration                 Configurations                                            Expected Active Configurations
        [InlineData("Debug|AnyCPU|net45",               "Debug|AnyCPU|net45",                                     "Debug|AnyCPU|net45")]
        [InlineData("Debug|AnyCPU|net45",               "Debug|AnyCPU|net45;Release|AnyCPU|net45",                "Debug|AnyCPU|net45")]
        [InlineData("Debug|AnyCPU|net45",               "Debug|AnyCPU|net45;Debug|AnyCPU|net46",                  "Debug|AnyCPU|net45;Debug|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU|net46",               "Debug|AnyCPU|net45;Debug|AnyCPU|net46",                  "Debug|AnyCPU|net45;Debug|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU|net45",                                     "Debug|AnyCPU|net45")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU|net45;Release|AnyCPU|net45",                "Debug|AnyCPU|net45")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU|net45;Debug|AnyCPU|net46",                  "Debug|AnyCPU|net45;Debug|AnyCPU|net46")]
        public async Task GetActiveProjectConfigurationsAsync_ConfigurationsWithTargetFrameworkDimensionProvider_ReturnsConfigsThatMatchConfigurationAndPlatformFromActiveConfiguration(string activeConfiguration, string configurations, string expected)
        {
            var provider = CreateInstance(activeConfiguration, configurations, "TargetFramework");

            var result = await provider.GetActiveProjectConfigurationsAsync();

            var activeConfigs = ProjectConfigurationFactory.CreateMany(expected.Split(';'));

            Assert.NotNull(result);
            Assert.Equal(activeConfigs.OrderBy(c => c.Name), result.Objects.OrderBy(c => c.Name));
            Assert.Equal(new[] { "TargetFramework" }, result.DimensionNames);
        }

        [Theory] // ActiveConfiguration                 Configurations
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU")]
        [InlineData("Debug|AnyCPU|net46",               "Debug|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU|net46",               "Debug|AnyCPU|net46;Release|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU;Release|AnyCPU")]
        [InlineData("Debug|AnyCPU",                     "Release|AnyCPU;Debug|AnyCPU")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU;Release|AnyCPU;Debug|x86")]
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU;Release|AnyCPU;Debug|x86;Release|x86")]
        [InlineData("Debug|AnyCPU",                     "Release|AnyCPU;Debug|x86;Release|x86;Debug|AnyCPU")]
        [InlineData("Release|AnyCPU",                   "Debug|AnyCPU;Release|AnyCPU")]
        [InlineData("Release|AnyCPU",                   "Release|AnyCPU;Debug|AnyCPU")]
        [InlineData("Debug|x86",                        "Debug|AnyCPU;Release|AnyCPU;Debug|x86")]
        [InlineData("Release|x86",                      "Debug|AnyCPU;Release|AnyCPU;Debug|x86;Release|x86")]
        [InlineData("Release|x86",                      "Release|AnyCPU;Debug|x86;Release|x86;Debug|AnyCPU")]
        public async Task GetActiveConfiguredProjects__WhenNoDimensionProviders_LoadsAndReturnsConfiguredProject(string activeConfiguration, string configurations)
        {
            var provider = CreateInstance(activeConfiguration, configurations);

            var result = await provider.GetActiveConfiguredProjectsAsync();

            Assert.NotNull(result);
            Assert.Single(result.Objects);
            Assert.Equal(activeConfiguration, result.Objects[0].ProjectConfiguration.Name);
            Assert.Empty(result.DimensionNames);
        }

        private static ActiveConfiguredProjectsProvider CreateInstance(string activeConfiguration, string configurations, params string[] dimensionNames)
        {
            var activeConfig = ProjectConfigurationFactory.Create(activeConfiguration);
            var configs = ProjectConfigurationFactory.CreateMany(configurations.Split(';'));
            var configurationsService = IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(configs.ToImmutableHashSet());
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => activeConfig);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider, projectConfigurationsService: configurationsService);
            var project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync((projectConfiguration) =>
            {
                return Task.FromResult(ConfiguredProjectFactory.ImplementProjectConfiguration(projectConfiguration));
            });

            var dimensionProviders = dimensionNames.Select(IActiveConfiguredProjectsDimensionProviderFactory.ImplementDimensionName);

            return CreateInstance(services: services, project: project, dimensionProviders: dimensionProviders);
        }

        private static ActiveConfiguredProjectsProvider CreateInstance(IUnconfiguredProjectServices? services = null, UnconfiguredProject? project = null, IEnumerable<IActiveConfiguredProjectsDimensionProvider>? dimensionProviders = null)
        {
            services ??= IUnconfiguredProjectServicesFactory.Create();
            project ??= UnconfiguredProjectFactory.Create();

            var provider = new ActiveConfiguredProjectsProvider(services, project);

            if (dimensionProviders is not null)
            {
                foreach (var dimensionProvider in dimensionProviders)
                {
                    provider.DimensionProviders.Add(dimensionProvider, appliesTo: ProjectCapabilities.AlwaysApplicable);
                }
            }

            return provider;
        }
    }
}
