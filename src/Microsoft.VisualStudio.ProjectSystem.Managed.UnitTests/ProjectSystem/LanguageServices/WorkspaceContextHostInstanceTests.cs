// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Xunit;

using static Microsoft.VisualStudio.ProjectSystem.LanguageServices.WorkspaceContextHost;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class WorkspaceContextHostInstanceTests
    {
        [Fact]
        public async Task Dispose_WhenNotInitialized_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenInitializedWithNoContext_DoesNotThrow()
        {
            var workspaceProjectContextProvider = IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(accessor: null);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: workspaceProjectContextProvider);

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task OpenContextForWriteAsync_WhenInitializedWithNoContext_ThrowsOperationCanceled()
        {
            var workspaceProjectContextProvider = IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(accessor: null);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: workspaceProjectContextProvider);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return instance.OpenContextForWriteAsync(context => Task.CompletedTask);
            });
        }

        [Fact]
        public async Task OpenContextForWriteAsync_WhenInitializedWithContext_CallsAction()
        {
            var accessor = IWorkspaceProjectContextAccessorFactory.Create();
            var workspaceProjectContextProvider = IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(accessor);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: workspaceProjectContextProvider);

            IWorkspaceProjectContextAccessor result = null;
            await instance.OpenContextForWriteAsync(a => { result = a; return Task.CompletedTask; });

            Assert.Same(accessor, result);
        }

        [Fact]
        public async Task OpenContextForWriteAsync_WhenProjectUnloaded_ThrowsOperationCanceled()
        {
            var tasksService = IUnconfiguredProjectTasksServiceFactory.ImplementUnloadCancellationToken(new CancellationToken(canceled: true));
            var accessor = IWorkspaceProjectContextAccessorFactory.Create();
            var workspaceProjectContextProvider = IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(accessor);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: workspaceProjectContextProvider, tasksService: tasksService);

            int callCount = 0;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            {
                return instance.OpenContextForWriteAsync(c => { callCount++; return Task.CompletedTask; });
            });

            Assert.Equal(0, callCount);
        }


        [Fact]
        public async Task InitializedAsync_WhenInitializedWithContext_RegistersContextWithTracker()
        {
            IWorkspaceProjectContext contextResult = null;
            string contextIdResult = null;
            var activeWorkspaceProjectContextTracker = IActiveEditorContextTrackerFactory.ImplementRegisterContext((c, id) => { contextResult = c; contextIdResult = id; });

            var context = IWorkspaceProjectContextMockFactory.Create();
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContext(context, "ContextId");
            var provider = new WorkspaceProjectContextProviderMock();
            provider.ImplementCreateProjectContextAsync(project => accessor);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: provider.Object, activeWorkspaceProjectContextTracker: activeWorkspaceProjectContextTracker);

            Assert.Same(context, contextResult);
            Assert.Equal("ContextId", contextIdResult);
        }

        [Fact]
        public async Task Dispose_WhenInitializedWithContext_ReleasesContext()
        {
            var accessor = IWorkspaceProjectContextAccessorFactory.Create();

            IWorkspaceProjectContextAccessor result = null;
            var provider = new WorkspaceProjectContextProviderMock();
            provider.ImplementCreateProjectContextAsync(project => accessor);
            provider.ImplementReleaseProjectContextAsync(a => { result = a; });

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: provider.Object);

            await instance.DisposeAsync();

            Assert.Same(accessor, result);
        }

        [Fact]
        public async Task Dispose_WhenInitializedWithContext_UnregistersContextWithTracker()
        {
            IWorkspaceProjectContext result = null;
            var activeWorkspaceProjectContextTracker = IActiveEditorContextTrackerFactory.ImplementReleaseContext(c => { result = c; });

            var context = IWorkspaceProjectContextMockFactory.Create();
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContext(context);
            var provider = new WorkspaceProjectContextProviderMock();
            provider.ImplementCreateProjectContextAsync(project => accessor);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: provider.Object, activeWorkspaceProjectContextTracker: activeWorkspaceProjectContextTracker);

            await instance.DisposeAsync();

            Assert.Same(context, result);
        }

        [Theory]
        [InlineData(true)]      // Evaluation
        [InlineData(false)]     // Project Build
        public async Task OnProjectChangedAsync_WhenProjectUnloaded_TriggersCancellation(bool evaluation)
        {
            var unloadSource = new CancellationTokenSource();
            var tasksService = IUnconfiguredProjectTasksServiceFactory.ImplementUnloadCancellationToken(unloadSource.Token);

            void applyChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> _, bool __, CancellationToken cancellationToken)
            {
                // Unload project
                unloadSource.Cancel();

                cancellationToken.ThrowIfCancellationRequested();
            }

            var applyChangesToWorkspaceContext = evaluation ? IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectEvaluationAsync(applyChanges) : IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectBuildAsync(applyChanges);

            var instance = await CreateInitializedInstanceAsync(tasksService: tasksService, applyChangesToWorkspaceContext: applyChangesToWorkspaceContext);

            var update = IProjectVersionedValueFactory.CreateEmpty();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return instance.OnProjectChangedAsync(update, evaluation);
            });
        }

        [Theory]
        [InlineData(true)]      // Evaluation
        [InlineData(false)]     // Project Build
        public async Task OnProjectChangedAsync_WhenInstanceDisposed_TriggersCancellation(bool evaluation)
        {
            WorkspaceContextHostInstance instance = null;

            void applyChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> _, bool __, CancellationToken cancellationToken)
            {
                // Dispose the instance underneath us
                instance.Dispose();

                cancellationToken.ThrowIfCancellationRequested();
            }

            var applyChangesToWorkspaceContext = evaluation ? IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectEvaluationAsync(applyChanges) : IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectBuildAsync(applyChanges);

            instance = await CreateInitializedInstanceAsync(applyChangesToWorkspaceContext: applyChangesToWorkspaceContext);

            var update = IProjectVersionedValueFactory.CreateEmpty();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return instance.OnProjectChangedAsync(update, evaluation);
            });
        }

        [Theory]
        [InlineData(true)]      // Evaluation
        [InlineData(false)]     // Project Build
        public async Task OnProjectChangedAsync_PassesProjectUpdate(bool evaluation)
        {
            IProjectVersionedValue<IProjectSubscriptionUpdate> updateResult = null;

            void applyChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> u, bool _, CancellationToken __)
            {
                updateResult = u;
            }

            var applyChangesToWorkspaceContext = evaluation ? IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectEvaluationAsync(applyChanges) : IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectBuildAsync(applyChanges);

            var instance = await CreateInitializedInstanceAsync(applyChangesToWorkspaceContext: applyChangesToWorkspaceContext);

            var update = IProjectVersionedValueFactory.CreateEmpty();
            await instance.OnProjectChangedAsync(update, evaluation);

            Assert.Same(updateResult, update);
        }

        [Theory] // Evaluation/Project Build       IsActiveContext
        [InlineData(true,                          true)]
        [InlineData(true,                          false)]
        [InlineData(false,                         true)]
        [InlineData(false,                         false)]
        public async Task OnProjectChangedAsync_RespectsIsActiveContext(bool evaluation, bool isActiveContext)
        {
            bool? isActiveContextResult = null;

            void applyChanges(IProjectVersionedValue<IProjectSubscriptionUpdate> u, bool iac, CancellationToken _)
            {
                isActiveContextResult = iac;
            }

            var activeWorkspaceProjectContextTracker = IActiveEditorContextTrackerFactory.ImplementIsActiveEditorContext(context => isActiveContext);
            var applyChangesToWorkspaceContext = evaluation ? IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectEvaluationAsync(applyChanges) : IApplyChangesToWorkspaceContextFactory.ImplementApplyProjectBuildAsync(applyChanges);

            var instance = await CreateInitializedInstanceAsync(applyChangesToWorkspaceContext: applyChangesToWorkspaceContext, activeWorkspaceProjectContextTracker: activeWorkspaceProjectContextTracker);

            var update = IProjectVersionedValueFactory.CreateEmpty();
            await instance.OnProjectChangedAsync(update, evaluation);

            Assert.Equal(isActiveContext, isActiveContextResult);
        }

        private async Task<WorkspaceContextHostInstance> CreateInitializedInstanceAsync(ConfiguredProject project = null, IProjectThreadingService threadingService = null, IUnconfiguredProjectTasksService tasksService = null, IProjectSubscriptionService projectSubscriptionService = null, IActiveEditorContextTracker activeWorkspaceProjectContextTracker = null, IWorkspaceProjectContextProvider workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext applyChangesToWorkspaceContext = null)
        {
            var instance = CreateInstance(project, threadingService, tasksService, projectSubscriptionService, activeWorkspaceProjectContextTracker, workspaceProjectContextProvider, applyChangesToWorkspaceContext);

            await instance.InitializeAsync();

            return instance;
        }

        private WorkspaceContextHostInstance CreateInstance(ConfiguredProject project = null, IProjectThreadingService threadingService = null, IUnconfiguredProjectTasksService tasksService = null, IProjectSubscriptionService projectSubscriptionService = null, IActiveEditorContextTracker activeWorkspaceProjectContextTracker = null, IWorkspaceProjectContextProvider workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext applyChangesToWorkspaceContext = null)
        {
            project = project ?? ConfiguredProjectFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();
            tasksService = tasksService ?? IUnconfiguredProjectTasksServiceFactory.Create();
            projectSubscriptionService = projectSubscriptionService ?? IProjectSubscriptionServiceFactory.Create();
            activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker ?? IActiveEditorContextTrackerFactory.Create();
            workspaceProjectContextProvider = workspaceProjectContextProvider ?? IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(IWorkspaceProjectContextAccessorFactory.Create());
            applyChangesToWorkspaceContext = applyChangesToWorkspaceContext ?? IApplyChangesToWorkspaceContextFactory.Create();

            return new WorkspaceContextHostInstance(project,
                                                    threadingService,
                                                    tasksService,
                                                    projectSubscriptionService,
                                                    workspaceProjectContextProvider,
                                                    activeWorkspaceProjectContextTracker,
                                                    ExportFactoryFactory.ImplementCreateValueWithAutoDispose(() => applyChangesToWorkspaceContext));
        }
    }
}
