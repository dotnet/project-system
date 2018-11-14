// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class WorkspaceContextHostTests
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
                return instance.OpenContextForWriteAsync((Func<IWorkspaceProjectContextAccessor, Task>)null);
            });
        }

        private WorkspaceContextHost CreateInstance(ConfiguredProject project = null, IProjectThreadingService threadingService = null, IUnconfiguredProjectTasksService tasksService = null, IProjectSubscriptionService projectSubscriptionService = null, IActiveEditorContextTracker activeWorkspaceProjectContextTracker = null, IWorkspaceProjectContextProvider workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext applyChangesToWorkspaceContext = null)
        {
            project = project ?? ConfiguredProjectFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();
            tasksService = tasksService ?? IUnconfiguredProjectTasksServiceFactory.Create();
            projectSubscriptionService = projectSubscriptionService ?? IProjectSubscriptionServiceFactory.Create();
            activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker ?? IActiveEditorContextTrackerFactory.Create();
            workspaceProjectContextProvider = workspaceProjectContextProvider ?? IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(IWorkspaceProjectContextAccessorFactory.Create());
            applyChangesToWorkspaceContext = applyChangesToWorkspaceContext ?? IApplyChangesToWorkspaceContextFactory.Create();

            return new WorkspaceContextHost(project,
                                            threadingService,
                                            tasksService,
                                            projectSubscriptionService,
                                            workspaceProjectContextProvider,
                                            activeWorkspaceProjectContextTracker,
                                            ExportFactoryFactory.ImplementCreateValueWithAutoDispose(() => applyChangesToWorkspaceContext));
        }
    }
}
