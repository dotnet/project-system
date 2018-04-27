// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export]
    [Export(typeof(IUnconfiguredProjectTasksService))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    internal class UnconfiguredProjectTasksService : IUnconfiguredProjectTasksService
    {
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IProjectThreadingService _threadingService;
        private readonly ILoadedInHostListener _loadedInHostListener;
        private readonly TaskCompletionSource<object> _projectLoadedInHost = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _prioritizedProjectLoadedInHost = new TaskCompletionSource<object>();
        private readonly JoinableTaskQueue _prioritizedTaskQueue;

        [ImportingConstructor]
        public UnconfiguredProjectTasksService([Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService, IProjectThreadingService threadingService, ILoadedInHostListener loadedInHostListener)
        {
            _prioritizedTaskQueue = new JoinableTaskQueue(threadingService.JoinableTaskContext);
            _tasksService = tasksService;
            _threadingService = threadingService;
            _loadedInHostListener = loadedInHostListener;
        }

        [ProjectAutoLoad(completeBy:ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
        public Task OnProjectFactoryCompleted()
        {
            return _loadedInHostListener.StartListeningAsync();
        }

        public Task ProjectLoadedInHost
        {
            get { return _projectLoadedInHost.Task.WithCancellation(_tasksService.UnloadCancellationToken); }
        }

        public Task PrioritizedProjectLoadedInHost
        {
            get { return _prioritizedProjectLoadedInHost.Task.WithCancellation(_tasksService.UnloadCancellationToken); }
        }

        public Task LoadedProjectAsync(Func<Task> action)
        {
            JoinableTask joinable = _tasksService.LoadedProjectAsync(action);

            return joinable.Task;
        }

        public Task<T> LoadedProjectAsync<T>(Func<Task<T>> action)
        {
            JoinableTask<T> joinable = _tasksService.LoadedProjectAsync(action);

            return joinable.Task;
        }

        public Task<T> PrioritizedProjectLoadedInHostAsync<T>(Func<Task<T>> action)
        {
            Requires.NotNull(action, nameof(action));

            _tasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

            JoinableTask<T> task = _threadingService.JoinableTaskFactory.RunAsync(action);
            _prioritizedTaskQueue.Register(task);

            return task.Task;
        }

        public Task PrioritizedProjectLoadedInHostAsync(Func<Task> action)
        {
            Requires.NotNull(action, nameof(action));

            _tasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

            JoinableTask task = _threadingService.JoinableTaskFactory.RunAsync(action);
            _prioritizedTaskQueue.Register(task);

            return task.Task;
        }

        public void OnProjectLoadedInHost()
        {
            _projectLoadedInHost.SetResult(null);
        }

        public void OnPrioritizedProjectLoadedInHost()
        {
            _prioritizedProjectLoadedInHost.SetResult(null);

            _threadingService.ExecuteSynchronously(() => _prioritizedTaskQueue.DrainAsync());
        }
    }
}
