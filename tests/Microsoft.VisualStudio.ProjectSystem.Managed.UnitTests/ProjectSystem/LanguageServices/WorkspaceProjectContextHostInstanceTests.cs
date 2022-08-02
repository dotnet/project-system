// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using static Microsoft.VisualStudio.ProjectSystem.LanguageServices.WorkspaceProjectContextHost;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class WorkspaceProjectContextHostInstanceTests
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
            var workspaceProjectContextProvider = IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(accessor: null!);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: workspaceProjectContextProvider);

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task OpenContextForWriteAsync_WhenInitializedWithNoContext_ThrowsOperationCanceled()
        {
            var workspaceProjectContextProvider = IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(accessor: null!);

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

            IWorkspaceProjectContextAccessor? result = null;
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
            string? contextIdResult = null;
            var activeWorkspaceProjectContextTracker = IActiveEditorContextTrackerFactory.ImplementRegisterContext(id => { contextIdResult = id; });

            var context = IWorkspaceProjectContextMockFactory.Create();
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContext(context, "ContextId");
            var provider = new WorkspaceProjectContextProviderMock();
            provider.ImplementCreateProjectContextAsync(project => accessor);

            await CreateInitializedInstanceAsync(workspaceProjectContextProvider: provider.Object, activeWorkspaceProjectContextTracker: activeWorkspaceProjectContextTracker);

            Assert.Equal("ContextId", contextIdResult);
        }

        [Fact]
        public async Task Dispose_WhenInitializedWithContext_ReleasesContext()
        {
            var accessor = IWorkspaceProjectContextAccessorFactory.Create();

            IWorkspaceProjectContextAccessor? result = null;
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
            string? result = null;
            var activeWorkspaceProjectContextTracker = IActiveEditorContextTrackerFactory.ImplementUnregisterContext(c => { result = c; });

            var context = IWorkspaceProjectContextMockFactory.Create();
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContext(context, "ContextId");
            var provider = new WorkspaceProjectContextProviderMock();
            provider.ImplementCreateProjectContextAsync(project => accessor);

            var instance = await CreateInitializedInstanceAsync(workspaceProjectContextProvider: provider.Object, activeWorkspaceProjectContextTracker: activeWorkspaceProjectContextTracker);

            await instance.DisposeAsync();

            Assert.Equal("ContextId", result);
        }

        [Fact]
        internal async Task OnProjectChangedAsync_WhenProjectUnloaded_TriggersCancellation()
        {
            var unloadSource = new CancellationTokenSource();
            
            var instance = await CreateInitializedInstanceAsync(
                tasksService: IUnconfiguredProjectTasksServiceFactory.ImplementUnloadCancellationToken(unloadSource.Token));

            var registration = IDataProgressTrackerServiceRegistrationFactory.Create();
            var activeConfiguredProject = ConfiguredProjectFactory.Create();
            var update = IProjectVersionedValueFactory.CreateEmpty();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return instance.OnProjectChangedAsync(
                    registration,
                    activeConfiguredProject,
                    update,
                    hasChange: _ => true,
                    applyFunc: (_, _, _, token) =>
                    {
                        // Simulate project unload during callback
                        unloadSource.Cancel();

                        token.ThrowIfCancellationRequested();
                    });
            });
        }

        [Fact]
        internal async Task OnProjectChangedAsync_WhenInstanceDisposed_TriggersCancellation()
        {
            var instance = await CreateInitializedInstanceAsync();
            var registration = IDataProgressTrackerServiceRegistrationFactory.Create();
            var activeConfiguredProject = ConfiguredProjectFactory.Create();
            var update = IProjectVersionedValueFactory.CreateEmpty();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return instance.OnProjectChangedAsync(
                    registration,
                    activeConfiguredProject,
                    update,
                    hasChange: _ => true,
                    applyFunc: (_, _, state, token) =>
                    {
                        // Dispose the instance underneath us
                        instance!.DisposeAsync().Wait();

                        token.ThrowIfCancellationRequested();
                    });
            });
        }

        [Fact]
        internal async Task OnProjectChangedAsync_CallsApplyFuncOnlyWhenChangeExists()
        {
            IImmutableDictionary<NamedIdentity, IComparable>? seenVersions = null;

            var instance = await CreateInitializedInstanceAsync();
            var registration = IDataProgressTrackerServiceRegistrationFactory.ImplementNotifyOutputDataCalculated(versions => seenVersions = versions);
            var activeConfiguredProject = ConfiguredProjectFactory.Create();
            var versions1 = ImmutableDictionary<NamedIdentity, IComparable>.Empty;
            var versions2 = ImmutableDictionary<NamedIdentity, IComparable>.Empty;
            var versions3 = ImmutableDictionary<NamedIdentity, IComparable>.Empty;
            var update1 = IProjectVersionedValueFactory.Create(versions1);
            var update2 = IProjectVersionedValueFactory.Create(versions2);
            var update3 = IProjectVersionedValueFactory.Create(versions3);
            var callCount = 0;

            // Apply func not called as no change
            await instance.OnProjectChangedAsync(
                registration,
                activeConfiguredProject,
                update1,
                hasChange: _ => false, // no change
                applyFunc: (_, _, state, token) => callCount++);

            Assert.Equal(0, callCount);
            Assert.Same(versions1, seenVersions);

            // Apply func will be called as hasChange returns true, despite the context state being unchanged
            await instance.OnProjectChangedAsync(
                registration,
                activeConfiguredProject,
                update2,
                hasChange: _ => true, // change
                applyFunc: (_, _, state, token) => callCount++);

            Assert.Equal(1, callCount);
            Assert.Same(versions2, seenVersions);

            // Apply func not called as no change
            await instance.OnProjectChangedAsync(
                registration,
                activeConfiguredProject,
                update3,
                hasChange: _ => false, // no change
                applyFunc: (_, _, state, token) => callCount++);

            Assert.Equal(1, callCount);
            Assert.Same(versions3, seenVersions);
        }

        [Theory]
        [CombinatorialData]
        internal async Task OnProjectChangedAsync_RespectsIsActiveContext(bool isActiveEditorContext, bool isActiveConfiguration)
        {
            var activeWorkspaceProjectContextTracker = IActiveEditorContextTrackerFactory.ImplementIsActiveEditorContext(context => isActiveEditorContext);

            var project = ConfiguredProjectFactory.Create();
            var activeConfiguredProject = isActiveConfiguration ? project : ConfiguredProjectFactory.Create();

            var instance = await CreateInitializedInstanceAsync(project: project, activeWorkspaceProjectContextTracker: activeWorkspaceProjectContextTracker);

            var registration = IDataProgressTrackerServiceRegistrationFactory.Create();
            var update = IProjectVersionedValueFactory.CreateEmpty();

            ContextState? observedState = null;

            await instance.OnProjectChangedAsync(
                registration,
                activeConfiguredProject,
                update,
                hasChange: _ => true,
                applyFunc: (_, _, state, token) => observedState = state);

            Assert.NotNull(observedState);
            Assert.Equal(isActiveEditorContext, observedState.Value.IsActiveEditorContext);
            Assert.Equal(isActiveConfiguration, observedState.Value.IsActiveConfiguration);
        }

        private static async Task<WorkspaceProjectContextHostInstance> CreateInitializedInstanceAsync(ConfiguredProject? project = null, IProjectThreadingService? threadingService = null, IUnconfiguredProjectTasksService? tasksService = null, IProjectSubscriptionService? projectSubscriptionService = null, IActiveEditorContextTracker? activeWorkspaceProjectContextTracker = null, IWorkspaceProjectContextProvider? workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext? applyChangesToWorkspaceContext = null)
        {
            var instance = CreateInstance(project, threadingService, tasksService, projectSubscriptionService, activeWorkspaceProjectContextTracker, workspaceProjectContextProvider, applyChangesToWorkspaceContext);

            await instance.InitializeAsync();

            return instance;
        }

        private static WorkspaceProjectContextHostInstance CreateInstance(ConfiguredProject? project = null, IProjectThreadingService? threadingService = null, IUnconfiguredProjectTasksService? tasksService = null, IProjectSubscriptionService? projectSubscriptionService = null, IActiveEditorContextTracker? activeEditorContextTracker = null, IWorkspaceProjectContextProvider? workspaceProjectContextProvider = null, IApplyChangesToWorkspaceContext? applyChangesToWorkspaceContext = null)
        {
            project ??= ConfiguredProjectFactory.Create();
            threadingService ??= IProjectThreadingServiceFactory.Create();
            tasksService ??= IUnconfiguredProjectTasksServiceFactory.Create();
            projectSubscriptionService ??= IProjectSubscriptionServiceFactory.Create();
            activeEditorContextTracker ??= IActiveEditorContextTrackerFactory.Create();
            workspaceProjectContextProvider ??= IWorkspaceProjectContextProviderFactory.ImplementCreateProjectContextAsync(IWorkspaceProjectContextAccessorFactory.Create());
            applyChangesToWorkspaceContext ??= IApplyChangesToWorkspaceContextFactory.Create();
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.Create();
            IDataProgressTrackerService dataProgressTrackerService = IDataProgressTrackerServiceFactory.Create();
            ICommandLineArgumentsProvider commandLineArgumentsProvider = ICommandLineArgumentsProviderFactory.Create();

            return new WorkspaceProjectContextHostInstance(project,
                                                           threadingService,
                                                           tasksService,
                                                           projectSubscriptionService,
                                                           workspaceProjectContextProvider,
                                                           activeEditorContextTracker,
                                                           activeConfiguredProjectProvider,
                                                           ExportFactoryFactory.ImplementCreateValueWithAutoDispose(() => applyChangesToWorkspaceContext),
                                                           dataProgressTrackerService,
                                                           commandLineArgumentsProvider);
        }
    }
}
