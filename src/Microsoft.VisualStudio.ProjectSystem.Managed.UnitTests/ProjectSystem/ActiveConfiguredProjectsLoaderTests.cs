// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class ActiveConfiguredProjectsLoaderTests
    {
        [Theory]
        [InlineData(new object[] { new[] { "Debug|x86" } })]
        [InlineData(new object[] { new[] { "Debug|x86", "Release|x86" } })]
        [InlineData(new object[] { new[] { "Debug|x86", "Release|x86", "Release|AnyCPU" } })]
        public async Task WhenActiveConfigurationChanges_LoadsConfiguredProject(string[] configurationNames)
        {
            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurationNames);

            var results = new List<string>();
            var project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync(configuration =>  {

                results.Add(configuration.Name);
                return Task.FromResult<ConfiguredProject>(null);
            });
            
            var loader = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            await loader.InitializeAsync();

            // Change the active configurations
            await source.SendAndCompleteAsync(configurationGroups, loader.TargetBlock);

            Assert.Equal(configurationNames, results);
        }

        [Fact]
        public void Dispose_WhenNotInitialized_DoesNotThrow()
        {
            var project = UnconfiguredProjectFactory.Create();

            var loader = CreateInstance(project, out _);
            loader.Dispose();

            Assert.True(loader.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenInitialized_DisposesSubscription()
        {
            int callCount = 0;
            UnconfiguredProject project = UnconfiguredProjectFactory.ImplementLoadConfiguredProjectAsync(configuration => {

                callCount++;
                return Task.FromResult<ConfiguredProject>(null);
            });

            var loader = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            await loader.InitializeAsync();
            loader.Dispose();

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            
            // Change the active configurations
            await source.SendAndCompleteAsync(configurationGroups, loader.TargetBlock);

            // Should not be listening
            Assert.Equal(0, callCount);
        }

        private static ActiveConfiguredProjectsLoader CreateInstance(UnconfiguredProject project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            source = ProjectValueDataSourceFactory.Create<IConfigurationGroup<ProjectConfiguration>>(services);
            var activeConfigurationGroupService = IActiveConfigurationGroupServiceFactory.Implement(source);

            var loader = CreateInstance(project, activeConfigurationGroupService);

            return loader;
        }

        private static ActiveConfiguredProjectsLoader CreateInstance(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService)
        {
            return new ActiveConfiguredProjectsLoader(project, activeConfigurationGroupService);
        }
    }
}
