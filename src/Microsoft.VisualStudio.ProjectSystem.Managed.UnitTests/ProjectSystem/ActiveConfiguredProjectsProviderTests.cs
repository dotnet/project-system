// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Configuration;
using Xunit;

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
        public async Task GetActiveProjectConfigurationsAsync_WhenNoDimensionlProviders_ReturnsNoDimensionNames()
        {
            var activeConfig = ProjectConfigurationFactory.Create("Debug|AnyCPU");
            var configs = ProjectConfigurationFactory.CreateMany("Debug|AnyCPU");
            var configurationsService = IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(configs);
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => activeConfig);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider, projectConfigurationsService: configurationsService);

            var provider = CreateInstance(services: services);

            var result = await provider.GetActiveProjectConfigurationsAsync();

            Assert.Empty(result.DimensionNames);
        }

        [Fact]
        public async Task GetActiveConfiguredProjectAsync_WhenNoDimensionlProviders_ReturnsNoDimensionNames()
        {
            var activeConfig = ProjectConfigurationFactory.Create("Debug|AnyCPU");
            var configs = ProjectConfigurationFactory.CreateMany("Debug|AnyCPU");
            var configurationsService = IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(configs);
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => activeConfig);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider, projectConfigurationsService: configurationsService);
            var configuredProject = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync((projectConfiguration) => {
                return Task.FromResult(ConfiguredProjectFactory.ImplementProjectConfiguration(projectConfiguration));
            });

            var commonServices = IUnconfiguredProjectCommonServicesFactory.ImplementProject(configuredProject);

            var provider = CreateInstance(services: services, commonServices: commonServices);

            var result = await provider.GetActiveConfiguredProjectsAsync();

            Assert.Empty(result.DimensionNames);

        }

        [Theory] // ActiveConfiguration                 Configurations
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU")]
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
        public async Task GetActiveProjectConfigurationsAsync_WhenNoDimensionlProviders_ReturnsActiveProjectConfiguration(string activeConfiguration, string configurations)
        {
            var activeConfig = ProjectConfigurationFactory.Create(activeConfiguration);
            var configs = ProjectConfigurationFactory.CreateMany(configurations.Split(';'));
            var configurationsService = IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(configs);
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => activeConfig);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider, projectConfigurationsService: configurationsService);

            var provider = CreateInstance(services: services);

            var result = await provider.GetActiveProjectConfigurationsAsync();

            Assert.Equal(1, result.Objects.Length);
            Assert.Equal(activeConfiguration, result.Objects[0].Name);
        }

        [Theory] // ActiveConfiguration                 Configurations                                            Expected Active Configurations
        [InlineData("Debug|AnyCPU|net45",               "Debug|AnyCPU|net45",                                     "Debug|AnyCPU|net45")]
        [InlineData("Debug|AnyCPU|net45",               "Debug|AnyCPU|net45;Release|AnyCPU|net45",                "Debug|AnyCPU|net45")]
        [InlineData("Debug|AnyCPU|net45",               "Debug|AnyCPU|net45;Debug|AnyCPU|net46",                  "Debug|AnyCPU|net45;Debug|AnyCPU|net46")]
        [InlineData("Debug|AnyCPU|net46",               "Debug|AnyCPU|net45;Debug|AnyCPU|net46",                  "Debug|AnyCPU|net45;Debug|AnyCPU|net46")]
        public async Task GetActiveProjectConfigurationsAsync_ConfigurationsWithTargetFrameworkDimensionProvider_ReturnsConfigsThatMatchConfigurationAndPlatformFromActiveConfiguration(string activeConfiguration, string configurations, string expected)
        {
            var activeConfig = ProjectConfigurationFactory.Create(activeConfiguration);
            var configs = ProjectConfigurationFactory.CreateMany(configurations.Split(';'));
            var configurationsService = IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(configs);
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => activeConfig);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider, projectConfigurationsService: configurationsService);
            var dimensionProvider = IActiveConfiguredProjectsDimensionProviderFactory.ImplementDimensionName("TargetFramework");
            var provider = CreateInstance(services: services);
            provider.DimensionProviders.Add(dimensionProvider, "");

            var result = await provider.GetActiveProjectConfigurationsAsync();

            var activeConfigs = ProjectConfigurationFactory.CreateMany(expected.Split(';'));
            Assert.Equal(activeConfigs, result.Objects);
            Assert.Equal(new[] { "TargetFramework" }, result.DimensionNames);
        }

        [Theory] // ActiveConfiguration                 Configurations
        [InlineData("Debug|AnyCPU",                     "Debug|AnyCPU")]
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
        public async Task GetActiveConfiguredProjects__WhenNoDimensionlProviders_LoadsAndReturnsConfiguredProject(string activeConfiguration, string configurations)
        {
            var activeConfig = ProjectConfigurationFactory.Create(activeConfiguration);
            var configs = ProjectConfigurationFactory.CreateMany(configurations.Split(';'));
            var configurationsService = IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(configs);
            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.ImplementActiveProjectConfiguration(() => activeConfig);
            var services = IUnconfiguredProjectServicesFactory.Create(activeConfiguredProjectProvider: activeConfiguredProjectProvider, projectConfigurationsService: configurationsService);
            var configuredProject = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync((projectConfiguration) => {
                return Task.FromResult(ConfiguredProjectFactory.ImplementProjectConfiguration(projectConfiguration));
            });

            var commonServices = IUnconfiguredProjectCommonServicesFactory.ImplementProject(configuredProject);

            var provider = CreateInstance(services: services, commonServices:commonServices);
            
            var result = await provider.GetActiveConfiguredProjectsAsync();

            Assert.Equal(1, result.Objects.Length);
            Assert.Equal(activeConfiguration, result.Objects[0].ProjectConfiguration.Name);
            Assert.Empty(result.DimensionNames);
        }

        private ActiveConfiguredProjectsProvider CreateInstance(IUnconfiguredProjectServices services = null, IUnconfiguredProjectCommonServices commonServices = null)
        {
            services = services ?? IUnconfiguredProjectServicesFactory.Create();
            commonServices = commonServices ?? IUnconfiguredProjectCommonServicesFactory.Create();

            return new ActiveConfiguredProjectsProvider(services, commonServices);
        }
    }
}
