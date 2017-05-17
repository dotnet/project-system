// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Abstraction over IProjectAsynchronousTasksService.LoadedProjectAsync(). 
    /// This simplifies unit testing of components that use LoadedProjectAsync
    /// </summary>
    [Export(typeof(ILoadedProjectContextService))]
    internal class LoadedProjectContextService : ILoadedProjectContextService
    {
        private readonly IProjectAsynchronousTasksService _tasksService;

        [ImportingConstructor]
        public LoadedProjectContextService(
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService)
        {
            _tasksService = tasksService;
        }

        /// <summary>
        /// Provides protection for some operation that the project will not close before 
        /// the completion of some task.
        /// </summary>
        public JoinableTask LoadedProjectAsync(Func<Task> action) =>
            _tasksService.LoadedProjectAsync(action);
    }
}
