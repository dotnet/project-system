// Copyright (c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class ConfiguredProjectImplicitActivationTrackingTests
    {
        [Fact]
        public async Task IsImplicitlyActive_WhenDisposed_ThrowsObjectDisposed()
        {
            var service = CreateInstance();
            await service.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _ = service.IsImplicitlyActive;
            });
        }

        [Fact]
        public async Task ImplicitlyActive_WhenDisposed_ThrowsObjectDisposed()
        {
            var service = CreateInstance();
            await service.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _ = service.ImplicitlyActive;
            });
        }

        [Fact]
        public void IsImplicitlyActive_WhenActiveConfigurationHasNotFired_ReturnsFalse()
        {
            var service = CreateInstance();

            var result = service.IsImplicitlyActive;

            Assert.False(result);
        }

        [Fact]
        public void ImplicitlyActive_WhenActiveConfiguredHasNotFired_ReturnsNonCompletedTask()
        {
            var service = CreateInstance();

            var result = service.ImplicitlyActive;

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void IsImplicitlyActive_WhenProjectHasUnloaded_ReturnsFalse()
        {
            var cancellationToken = new CancellationToken(canceled: true);
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationToken);

            var service = CreateInstance(tasksService);

            var result = service.IsImplicitlyActive;

            Assert.False(result);
        }

        [Fact]
        public void ImplicitlyActive_WhenProjectHasUnloaded_ReturnsCanceledTask()
        {
            var cancellationToken = new CancellationToken(canceled: true);
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationToken);

            var service = CreateInstance(tasksService);

            var result = service.ImplicitlyActive;

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void IsImplicitlyActive_WhenProjectUnloadCancellationTokenSourceHasBeenDisposed_ReturnsFalse()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.IsImplicitlyActive;

            Assert.False(result);
        }

        [Fact]
        public void ImplicitlyActive_WhenProjectUnloadCancellationTokenSourceHasBeenDisposed_ReturnsNonCompletedTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.ImplicitlyActive;

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void IsImplicitlyActive_WhenProjectUnloadCancellationTokenSourceHasBeenCanceledAndDisposed_ReturnsFalse()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.IsImplicitlyActive;

            Assert.False(result);
        }

        [Fact]
        public void ImplicitlyActive_WhenProjectUnloadCancellationTokenSourceHasBeenCanceledAndDisposed_ReturnsCanceledTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.ImplicitlyActive;

            Assert.True(result.IsCanceled);
        }

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
        public async Task IsImplicitlyActive_WhenActiveConfigurationChangesAndMatches_ReturnsTrue(string[] configurations, string currentConfiguration)
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(currentConfiguration);
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);

            Assert.False(service.IsImplicitlyActive); // Just to init

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurations);
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.True(service.IsImplicitlyActive);
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
        public async Task ImplicitlyActive_WhenActiveConfigurationChangesAndMatches_ReturnsCompletedTask(string[] configurations, string currentConfiguration)
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(currentConfiguration);
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);

            var result = service.ImplicitlyActive;

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurations);
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.True(result.Status == TaskStatus.RanToCompletion);
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
        public async Task IsImplicitlyActive_WhenAccessedInActivateAsync_ReturnsTrue()
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            service.Load();

            bool? result = null;
            var implicitActiveService = IImplicitlyActiveServiceFactory.ImplementActivateAsync(() =>
            {
                result = service.IsImplicitlyActive;
            });

            service.ImplicitlyActiveServices.Add(implicitActiveService);

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.True(result);
        }

        [Fact]
        public async Task IsImplicitlyActive_WhenAccessedInDeactivateAsync_ReturnsFalse()
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            service.Load();

            Assert.False(service.IsImplicitlyActive);

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            await source.SendAsync(configurationGroups);
            await Task.Delay(500);  // Wait for data to be sent

            Assert.True(service.IsImplicitlyActive);

            bool? result = null;
            var implicitActiveService = IImplicitlyActiveServiceFactory.ImplementDeactivateAsync(() =>
            {
                result = service.IsImplicitlyActive;
            });

            service.ImplicitlyActiveServices.Add(implicitActiveService);

            configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|x86");
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.False(result);
        }

        [Fact]
        public async Task WhenActiveConfigurationChangesAndNoLongerMatches_CallsDeactivateAsync()
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);
            service.Load();

            Assert.False(service.IsImplicitlyActive);

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            await source.SendAsync(configurationGroups);
            await Task.Delay(500);  // Wait for data to be sent

            Assert.True(service.IsImplicitlyActive);

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

        [Fact]
        public async Task ImplicitlyActive_CompletedStateChangesOverLifetime()
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);

            Assert.False(service.IsImplicitlyActive);

            // Should now be considered active
            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            await source.SendAsync(configurationGroups);

            Assert.True(service.ImplicitlyActive.Wait(500));

            configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|x86");
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            // Should now be considered in-active
            Assert.False(service.ImplicitlyActive.IsCompleted);
        }

        private static ConfiguredProjectImplicitActivationTracking CreateInstance()
        {
            return CreateInstance(null, null, out _);
        }

        private static ConfiguredProjectImplicitActivationTracking CreateInstance(ConfiguredProject project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            return CreateInstance(project, null, out source);
        }

        private static ConfiguredProjectImplicitActivationTracking CreateInstance(IProjectAsynchronousTasksService tasksService)
        {
            return CreateInstance(null, tasksService, out _);
        }

        private static ConfiguredProjectImplicitActivationTracking CreateInstance(ConfiguredProject? project, IProjectAsynchronousTasksService? tasksService, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            project ??= ConfiguredProjectFactory.Create();
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            source = ProjectValueDataSourceFactory.Create<IConfigurationGroup<ProjectConfiguration>>(services);
            var activeConfigurationGroupService = IActiveConfigurationGroupServiceFactory.Implement(source);

            tasksService ??= IProjectAsynchronousTasksServiceFactory.Create();

            return new ConfiguredProjectImplicitActivationTracking(project, activeConfigurationGroupService, tasksService);
        }
    }
}
