// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Abstraction over IProjectAsynchronousTasksService.LoadedProjectAsync(). 
    /// This simplifies unit testing of components that use LoadedProjectAsync
    /// </summary>
    internal interface ILoadedProjectContextService
    {
        /// <summary>
        /// Provides protection for some operation that the project will not close before 
        /// the completion of some task.
        /// </summary>
        JoinableTask LoadedProjectAsync(Func<Task> action);
    }
}
