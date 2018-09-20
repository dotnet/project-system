// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class WorkspaceContextHostTests
    {
        [Fact]
        public void Loaded_WhenNotActivated_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            Assert.False(component.Loaded.IsCanceled);
            Assert.False(component.Loaded.IsCompleted);
            Assert.False(component.Loaded.IsFaulted);
        }

        [Fact]
        public async Task Loaded_WhenActivated_ReturnsCompletedTask()
        {
            var component = CreateInstance();

            await component.ActivateAsync();

            Assert.True(component.Loaded.IsCompleted);
        }

        [Fact]
        public async Task Loaded_WhenDeactivated_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            await component.ActivateAsync();
            await component.UnloadAsync();

            Assert.False(component.Loaded.IsCanceled);
            Assert.False(component.Loaded.IsCompleted);
            Assert.False(component.Loaded.IsFaulted);
        }

        [Fact]
        public async Task Loaded_DisposedWhenNotActivated_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.DisposeAsync();

            Assert.True(component.Loaded.IsCanceled);
        }

        [Fact]
        public async Task Loaded_DisposedWhenActivated_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.ActivateAsync();
            await component.DisposeAsync();

            Assert.True(component.Loaded.IsCanceled);
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
                return instance.OpenContextForWriteAsync((Func<IWorkspaceProjectContext, Task>)null);
            });
        }

        private WorkspaceContextHost CreateInstance(ConfiguredProject project = null, IProjectThreadingService threadingService = null, IUnconfiguredProjectTasksService tasksService = null, IProjectSubscriptionService projectSubscriptionService = null, IActiveWorkspaceProjectContextTracker activeWorkspaceProjectContextTracker = null, IWorkspaceProjectContextProvider workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext applyChangesToWorkspaceContext = null)
        {
            project = project ?? ConfiguredProjectFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();
            tasksService = tasksService ?? IUnconfiguredProjectTasksServiceFactory.Create();
            projectSubscriptionService = projectSubscriptionService ?? IProjectSubscriptionServiceFactory.Create();
            activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker ?? IActiveWorkspaceProjectContextTrackerFactory.Create();
            workspaceProjectContextProvider = workspaceProjectContextProvider ?? IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(IWorkspaceProjectContextMockFactory.Create());
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
