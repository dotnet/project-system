// Copyright (c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class ActiveConfiguredProjectServicesTests
    {
        [Fact]
        public async Task IsActive_WhenDisposed_ThrowsObjectDisposed()
        {
            var service = CreateInstance();
            await service.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                var ignored = service.IsActive;
            });
        }

        [Fact]
        public async Task IsActiveTask_WhenDisposed_ThrowsObjectDisposed()
        {
            var service = CreateInstance();
            await service.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                var ignored = service.IsActiveTask;
            });
        }

        [Fact]
        public void IsActive_WhenActiveConfigurationHasNotFired_ReturnsFalse()
        {
            var service = CreateInstance();

            var result = service.IsActive;

            Assert.False(result);
        }

        [Fact]
        public void IsActiveTask_WhenActiveConfiguredHasNotFired_ReturnsNonCompletedTask()
        {
            var service = CreateInstance();

            var result = service.IsActiveTask;

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void IsActive_WhenProjectHasUnloaded_ReturnsFalse()
        {
            var cancellationToken = new CancellationToken(canceled: true);
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationToken);

            var service = CreateInstance(tasksService);

            var result = service.IsActive;

            Assert.False(result);
        }

        [Fact]
        public void IsActiveTask_WhenProjectHasUnloaded_ReturnsCanceledTask()
        {
            var cancellationToken = new CancellationToken(canceled: true);
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationToken);

            var service = CreateInstance(tasksService);

            var result = service.IsActiveTask;

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void IsActive_WhenProjectUnloadCancellationTokenSourceHasBeenDisposed_ReturnsFalse()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.IsActive;

            Assert.False(result);
        }

        [Fact]
        public void IsActiveTask_WhenProjectUnloadCancellationTokenSourceHasBeenDisposed_ReturnsNonCompletedTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.IsActiveTask;

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void IsActive_WhenProjectUnloadCancellationTokenSourceHasBeenCanceledAndDisposed_ReturnsFalse()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.IsActive;

            Assert.False(result);
        }

        [Fact]
        public void IsActiveTask_WhenProjectUnloadCancellationTokenSourceHasBeenCanceledAndDisposed_ReturnsCanceledTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            var service = CreateInstance(tasksService);

            var result = service.IsActiveTask;

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
        public async Task IsActive_WhenActionConfigurationChangesAndMatches_ReturnsTrue(string[] configurations, string currentConfiguration)
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(currentConfiguration);
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);

            Assert.False(service.IsActive); // Just to init

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurations);
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.True(service.IsActive);
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
        public async Task IsActiveTask_WhenActionConfigurationChangesAndMatches_ReturnsCompletedTask(string[] configurations, string currentConfiguration)
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(currentConfiguration);
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);

            var result = service.IsActiveTask;

            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames(configurations);
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task IsActiveTask_CompletedStateChangesOverLifetime()
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");
            var service = CreateInstance(project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source);

            Assert.False(service.IsActive);

            // Should now be considered active
            var configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|AnyCPU");
            await source.SendAsync(configurationGroups);

            Assert.True(service.IsActiveTask.Wait(500));

            configurationGroups = IConfigurationGroupFactory.CreateFromConfigurationNames("Debug|x86");
            await source.SendAndCompleteAsync(configurationGroups, service.TargetBlock);

            // Should now be considered in-active
            Assert.False(service.IsActiveTask.IsCompleted);
        }

        private static ActiveConfiguredProjectService CreateInstance()
        {
            return CreateInstance(null, null, out _);
        }

        private static ActiveConfiguredProjectService CreateInstance(ConfiguredProject project, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            return CreateInstance(project, null, out source);
        }

        private static ActiveConfiguredProjectService CreateInstance(IProjectAsynchronousTasksService tasksService)
        {
            return CreateInstance(null, tasksService, out _);
        }

        private static ActiveConfiguredProjectService CreateInstance(ConfiguredProject project, IProjectAsynchronousTasksService tasksService, out ProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            project = project ?? ConfiguredProjectFactory.Create();
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            source = ProjectValueDataSourceFactory.Create<IConfigurationGroup<ProjectConfiguration>>(services);
            var activeConfigurationGroupService = IActiveConfigurationGroupServiceFactory.Implement(source);

            tasksService= tasksService ?? IProjectAsynchronousTasksServiceFactory.Create();

            return new ActiveConfiguredProjectService(project, activeConfigurationGroupService, tasksService);
        }
    }
}
