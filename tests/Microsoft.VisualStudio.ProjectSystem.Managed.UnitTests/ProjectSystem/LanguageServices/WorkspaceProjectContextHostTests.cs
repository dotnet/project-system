// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class WorkspaceProjectContextHostTests
    {
        [Fact]
        public void PublishAsync_WhenNotActivated_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            var result = component.PublishAsync();

            Assert.False(result.IsCanceled);
            Assert.False(result.IsCompleted);
            Assert.False(result.IsFaulted);
        }

        [Fact]
        public async Task PublishAsync_WhenActivated_ReturnsCompletedTask()
        {
            var component = CreateInstance();

            await component.ActivateAsync();

            var result = component.PublishAsync();

            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task PublishAsync_WhenDeactivated_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            await component.ActivateAsync();
            await component.UnloadAsync();

            var result = component.PublishAsync();

            Assert.False(result.IsCanceled);
            Assert.False(result.IsCompleted);
            Assert.False(result.IsFaulted);
        }

        [Fact]
        public async Task PublishAsync_DisposedWhenNotActivated_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.DisposeAsync();

            var result = component.PublishAsync();

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task PublishAsync_DisposedWhenActivated_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.ActivateAsync();
            await component.DisposeAsync();

            var result = component.PublishAsync();

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task Dispose_WhenNotActivated_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenActivated_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.ActivateAsync();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenDeactivated_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.ActivateAsync();
            await instance.DeactivateAsync();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task OpenContextForWriteAsync_NullAsAction_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            await instance.ActivateAsync();

            await Assert.ThrowsAsync<ArgumentNullException>("action", () =>
            {
                return instance.OpenContextForWriteAsync(null!);
            });
        }

        private static WorkspaceProjectContextHost CreateInstance(ConfiguredProject? project = null, IProjectThreadingService? threadingService = null, IUnconfiguredProjectTasksService? tasksService = null, IProjectSubscriptionService? projectSubscriptionService = null, IActiveEditorContextTracker? activeWorkspaceProjectContextTracker = null, IWorkspaceProjectContextProvider? workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext? applyChangesToWorkspaceContext = null)
        {
            project ??= ConfiguredProjectFactory.Create();
            threadingService ??= IProjectThreadingServiceFactory.Create();
            tasksService ??= IUnconfiguredProjectTasksServiceFactory.Create();
            projectSubscriptionService ??= IProjectSubscriptionServiceFactory.Create();
            activeWorkspaceProjectContextTracker ??= IActiveEditorContextTrackerFactory.Create();
            workspaceProjectContextProvider ??= IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(IWorkspaceProjectContextAccessorFactory.Create());
            applyChangesToWorkspaceContext ??= IApplyChangesToWorkspaceContextFactory.Create();
            IDataProgressTrackerService dataProgressTrackerService = IDataProgressTrackerServiceFactory.Create();
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.Create();
            IProjectBuildSnapshotService projectBuildSnapshotService = IProjectBuildSnapshotServiceFactory.Create();

            return new WorkspaceProjectContextHost(project,
                                                   threadingService,
                                                   tasksService,
                                                   projectSubscriptionService,
                                                   workspaceProjectContextProvider,
                                                   activeWorkspaceProjectContextTracker,
                                                   activeConfiguredProjectProvider,
                                                   ExportFactoryFactory.ImplementCreateValueWithAutoDispose(() => applyChangesToWorkspaceContext),
                                                   dataProgressTrackerService,
                                                   projectBuildSnapshotService);
        }
    }
}
