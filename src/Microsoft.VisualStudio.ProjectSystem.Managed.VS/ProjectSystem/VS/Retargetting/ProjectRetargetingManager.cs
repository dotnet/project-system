// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectRetargetingManager))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class ProjectRetargetingManager : IProjectRetargetingManager
    {
        private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _retargettingService;
        private readonly IProjectThreadingService _threadingService;
        private ITaskDelayScheduler? _taskDelayScheduler;

        private ImmutableList<string> _projectsToWaitFor = ImmutableList<string>.Empty;
        private bool _needRetarget = false;

        [ImportingConstructor]
        public ProjectRetargetingManager(IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService,
                                         IProjectThreadingService threadingService)
        {
            _retargettingService = retargettingService;
            _threadingService = threadingService;
            _taskDelayScheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), threadingService, default);
        }

        public void ReportProjectMightRetarget(string projectFile)
        {
            ThreadingTools.ApplyChangeOptimistically(ref _projectsToWaitFor, col =>
            {
                if (!col.Contains(projectFile))
                {
                    col = col.Add(projectFile);
                }

                return col;
            });
        }

        public void ReportProjectNeedsRetargeting(string projectFile, bool needsRetarget)
        {
            ThreadingTools.ApplyChangeOptimistically(ref _projectsToWaitFor, col =>
            {
                if (col.Contains(projectFile))
                {
                    col = col.Remove(projectFile);
                }

                return col;
            });

            if (needsRetarget)
            {
                _needRetarget = true;
            }

            if (_projectsToWaitFor.Count == 0 && _needRetarget && _taskDelayScheduler != null)
            {
                _ = _taskDelayScheduler.ScheduleAsyncTask(AllProjectsDoneLoading, default);
            }
        }

        public async Task AllProjectsDoneLoading(CancellationToken cancellationToken)
        {
            _taskDelayScheduler = null;

            await _threadingService.SwitchToUIThread();

            IVsTrackProjectRetargeting2 service = await _retargettingService.GetValueAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            ErrorHandler.ThrowOnFailure(service.CheckSolutionForRetarget((uint)__RETARGET_CHECK_OPTIONS.RCO_FIRST_SOLUTION_LOAD));
        }
    }
}
