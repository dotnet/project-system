// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class ConfiguredProjectImplicitActivationTrackingTests
    {
        [Fact]
        public async Task Dispose_WhenNotInitialized_DoesNotThrow()
        {
            var service = CreateInstance();
            await service.DisposeAsync();

            Assert.True(service.IsDisposed);
        }

        [Theory]                           // Active configs                                                         Current
        [InlineData(new object[] { new[] { "Debug|x86" },                                                            "Debug|x86" })]
        [InlineData(new object[] { new[] { "Debug|x86", "Release|x86" },                                             "Debug|x86" })]
        [InlineData(new object[] { new[] { "Debug|x86", "Release|x86", "Release|AnyCPU" },                           "Debug|x86" })]
        [InlineData(new object[] { new[] { "Release|x86", "Debug|x86" },                                             "Debug|x86" })]
        [InlineData(new object[] { new[] { "Release|x86", "Release|AnyCPU", "Debug|x86" },                           "Debug|x86" })]
        [InlineData(new object[] { new[] { "Debug|x86|net46" },                                                      "Debug|x86|net46" })]
        [InlineData(new object[] { new[] { "Debug|x86|net46", "Release|x86|net46" },                                 "Debug|x86|net46" })]
        [InlineData(new object[] { new[] { "Debug|x86|net46", "Release|x86|net46", "Release|AnyCPU|net46" },         "Debug|x86|net46" })]
        public async Task WhenActiveConfigurationChangesAndMatches_CallsActivateAsync(string[] configurations, string currentConfiguration)
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(currentConfiguration);
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            service.Load();

            int callCount = 0;
            var implicitActiveService = IImplicitlyActiveServiceFactory.ImplementActivateAsync(() =>
            {
                callCount++;
            });

            service.ImplicitlyActiveServices.Add(implicitActiveService);

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurations);
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task WhenActiveConfigurationChangesAndNoLongerMatches_CallsDeactivateAsync()
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            service.Load();

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            await source.SendAsync(configurationGroups);
            await Task.Delay(500);  // Wait for data to be sent

            int callCount = 0;
            var implicitActiveService = IImplicitlyActiveServiceFactory.ImplementDeactivateAsync(() =>
            {
                callCount++;
            });

            service.ImplicitlyActiveServices.Add(implicitActiveService);

            configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|x86");
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.Equal(1, callCount);
        }

        private static ConfiguredProjectImplicitActivationTracking CreateInstance()
        {
            return CreateInstance(null, out _);
        }

        private static ConfiguredProjectImplicitActivationTracking CreateInstance(ConfiguredProject? project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            project ??= ConfiguredProjectFactory.Create();
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            source = ProjectValueDataSourceFactory.Create<IConfigurationGroup<ProjectConfiguration>>(services);
            var activeConfigurationGroupService = IActiveConfigurationGroupServiceFactory.Implement(source);

            return new ConfiguredProjectImplicitActivationTracking(project, activeConfigurationGroupService);
        }
    }
}
