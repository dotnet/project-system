// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IUnconfiguredProjectTasksService))]
    internal class UnconfiguredProjectTasksService : IUnconfiguredProjectTasksService
    {
        private readonly IProjectAsynchronousTasksService _tasksService;

        [ImportingConstructor]
        public UnconfiguredProjectTasksService([Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService)
        {
            _tasksService = tasksService;
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
    }
}
