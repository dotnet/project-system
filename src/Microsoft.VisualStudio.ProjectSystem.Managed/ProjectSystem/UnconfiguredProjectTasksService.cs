// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private readonly ILoadedInHostListener _loadedInHostListener;
        private readonly TaskCompletionSource<object> _projectLoadedInHost = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _prioritizedProjectLoadedInHost = new TaskCompletionSource<object>();

        [ImportingConstructor]
        public UnconfiguredProjectTasksService([Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService, ILoadedInHostListener loadedInHostListener)
        {
            _tasksService = tasksService;
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

        public void OnProjectLoadedInHost()
        {
            _projectLoadedInHost.SetResult(null);
        }

        public void OnPrioritizedProjectLoadedInHost()
        {
            _prioritizedProjectLoadedInHost.SetResult(null);
        }
    }
}
