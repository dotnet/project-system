// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Trait("UnitTest", "ProjectSystem")]
    public class ActiveConfiguredProjectsLoaderTests
    {
        [Theory]
        [InlineData(new object[] { new[] { "Debug|x86" } })]
        [InlineData(new object[] { new[] { "Debug|x86", "Release|x86" } })]
        [InlineData(new object[] { new[] { "Debug|x86", "Release|x86", "Release|AnyCPU" } })]
        public async Task WhenActiveConfigurationChanges_LoadsConfiguredProject(string[] configurationNames)
        {
            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurationNames);
            var e = ProjectVersionedValueFactory.Create(configurationGroups);

            var results = new List<string>();
            var project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync(configuration =>
            {
                results.Add(configuration.Name);
                return Task.FromResult<ConfiguredProject>(null);
            });

            var loader = CreateInstance(project);
            await loader.InitializeAsync();

            await loader.OnActiveConfigurationsChangedAsync(e);

            Assert.Equal(configurationNames, results);
        }

        [Fact]
        public async Task WhenProjectUnloading_DoesNotLoadConfiguredProject()
        {
            var tasksService = IUnconfiguredProjectTasksServiceFactory.CreateWithUnloadedProject<ConfiguredProject>();

            int callCount = 0;
            UnconfiguredProject project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync(configuration =>
            {
                callCount++;
                return Task.FromResult<ConfiguredProject>(null);
            });

            var loader = CreateInstance(project, tasksService);
            await loader.InitializeAsync();

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            var e = ProjectVersionedValueFactory.Create(configurationGroups);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return loader.OnActiveConfigurationsChangedAsync(e);
            });

            // Should not be listening
            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task InitializeAsync_CanNotInitializeTwice()
        {
            var results = new List<string>();
            var project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync(configuration =>
            {

                results.Add(configuration.Name);
                return Task.FromResult<ConfiguredProject>(null);
            });

            var loader = CreateInstance(project);

            await loader.InitializeAsync();
            await loader.InitializeAsync();

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            var e = ProjectVersionedValueFactory.Create(configurationGroups);

            await loader.OnActiveConfigurationsChangedAsync(e);

            Assert.Equal(new string[] { "Debug|AnyCPU" }, results);
        }

        [Fact]
        public void Dispose_WhenNotInitialized_DoesNotThrow()
        {
            var project = UnconfiguredProjectFactory.Create();

            var loader = CreateInstance(project);
            loader.Dispose();

            Assert.True(loader.IsDisposed);
        }

        [Fact]
        public async Task WhenDisposed_DoesNotLoadConfiguredProjec()
        {
            var tasksService = IUnconfiguredProjectTasksServiceFactory.CreateWithUnloadedProject<ConfiguredProject>();

            int callCount = 0;
            UnconfiguredProject project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync(configuration =>
            {
                callCount++;
                return Task.FromResult<ConfiguredProject>(null);
            });

            var loader = CreateInstance(project, tasksService);
            await loader.InitializeAsync();
            loader.Dispose();

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            var e = ProjectVersionedValueFactory.Create(configurationGroups);

            await loader.OnActiveConfigurationsChangedAsync(e);

            Assert.Equal(0, callCount);

        }

        private static ActiveConfiguredProjectsLoader CreateInstance(UnconfiguredProject project, IUnconfiguredProjectTasksService tasksService = null)
        {
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            var source = ProjectValueDataSourceFactory.Create<IConfigurationGroup<ProjectConfiguration>>(services);
            var activeConfigurationGroupService = IActiveConfigurationGroupServiceFactory.Implement(source);

            var loader = CreateInstance(project, activeConfigurationGroupService, tasksService);

            return loader;
        }

        private static ActiveConfiguredProjectsLoader CreateInstance(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService, IUnconfiguredProjectTasksService tasksService)
        {
            tasksService = tasksService ?? IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync<ConfiguredProject>(t => t());

            return new ActiveConfiguredProjectsLoader(project, activeConfigurationGroupService, tasksService);
        }
    }
}
