// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     An implementation of <see cref="ISafeProjectGuidService"/> that waits until the project
    ///     has been loaded into the host environment before returning the project GUID.
    /// </summary>
    [Export(typeof(ISafeProjectGuidService))]
    internal class VsSafeProjectGuidService : ISafeProjectGuidService
    {
        private readonly UnconfiguredProject _project;
        private readonly IUnconfiguredProjectTasksService _tasksService;

        [ImportingConstructor]
        public VsSafeProjectGuidService(UnconfiguredProject project, IUnconfiguredProjectTasksService tasksService)
        {
            _project = project;
            _tasksService = tasksService;
        }

        public async Task<Guid> GetProjectGuidAsync(CancellationToken cancellationToken = default)
        {
            await _tasksService.PrioritizedProjectLoadedInHost.WithCancellation(cancellationToken);

            return await _project.GetProjectGuidAsync();
        }
    }
}
