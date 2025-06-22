// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

/// <summary>
///     An implementation of <see cref="ISafeProjectGuidService"/> that waits until the project
///     has been loaded into the host environment before returning the project GUID.
/// </summary>
[Export(typeof(ISafeProjectGuidService))]
internal class VsSafeProjectGuidService : ISafeProjectGuidService
{
    private readonly UnconfiguredProject _project;
    private readonly IUnconfiguredProjectTasksService _tasksService;
    private readonly IVsService<IVsBackgroundSolution> _backgroundSolutionImport;

    [ImportingConstructor]
    public VsSafeProjectGuidService(
        UnconfiguredProject project,
        IUnconfiguredProjectTasksService tasksService,
        IVsService<SVsBackgroundSolution, IVsBackgroundSolution> backgroundSolutionImport)
    {
        _project = project;
        _tasksService = tasksService;
        _backgroundSolutionImport = backgroundSolutionImport;
    }

    public Task<Guid> GetProjectGuidAsync(CancellationToken cancellationToken = default)
    {
        if (!_tasksService.PrioritizedProjectLoadedInHost.IsCompleted)
        {
            return GetProjectGuidSlowAsync(cancellationToken);
        }

        return _project.GetProjectGuidAsync();

        async Task<Guid> GetProjectGuidSlowAsync(CancellationToken cancellationToken)
        {
            Guid projectGuid = await _project.GetProjectGuidAsync();

            // if this is a newly created project, the project GUID might be empty.
            // we should wait until the project is added to the solution.
            if (projectGuid != Guid.Empty)
            {
                // now, get the GUID recorded by the solution.
                // the solution might not know the project, if this is a project just to be added to the solution.
                // or if the project saves a different GUID, we need wait the solution to resolve this conflict.
                IVsBackgroundSolution solution = await _backgroundSolutionImport.GetValueAsync(cancellationToken);
                if (solution.GetProjectGuidFromAbsolutePath(_project.FullPath) == projectGuid)
                {
                    // If the project GUID matches the one from the solution, we can return it immediately.
                    return projectGuid;
                }
            }

            await _tasksService.PrioritizedProjectLoadedInHost.WithCancellation(cancellationToken);
            return await _project.GetProjectGuidAsync();
        }
    }
}
