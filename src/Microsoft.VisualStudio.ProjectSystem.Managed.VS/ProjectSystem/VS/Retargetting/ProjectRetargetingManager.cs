// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectRetargetingManager))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class ProjectRetargetingManager : IProjectRetargetingManager, IVsSolutionEvents, IDisposable
    {
        private readonly IVsService<SVsSolution, IVsSolution> _solutionService;
        private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _retargettingService;
        private readonly IProjectThreadingService _threadingService;
        private readonly ITaskDelayScheduler _taskDelayScheduler;
        private IVsSolution? _solution;
        private bool _solutionOpened;

        private ImmutableHashSet<IVsProjectTargetDescription> _registeredDescriptions = ImmutableHashSet<IVsProjectTargetDescription>.Empty;
        private ImmutableList<string> _projectsToWaitFor = ImmutableList<string>.Empty;
        private bool _needRetarget = false;
        private uint _cookie;

        [ImportingConstructor]
        public ProjectRetargetingManager(IVsService<SVsSolution, IVsSolution> solutionService,
                                         IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService,
                                         IProjectThreadingService threadingService)
        {
            _solutionService = solutionService;
            _retargettingService = retargettingService;
            _threadingService = threadingService;

            _taskDelayScheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), threadingService, default);
        }

        public void Dispose()
        {
            if (_cookie != VSConstants.VSCOOKIE_NIL && _solution != null)
            {
                Verify.HResult(_solution.UnadviseSolutionEvents(_cookie));

                _cookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        public void ReportProjectMightRetarget(string projectFile)
        {
            if (_solution == null)
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    await _threadingService.SwitchToUIThread();
                    if (_solution != null) return;

                    _solution = await _solutionService.GetValueAsync();

                    Verify.HResult(_solution.AdviseSolutionEvents(this, out _cookie));
                });
            }

            ThreadingTools.ApplyChangeOptimistically(ref _projectsToWaitFor, col =>
            {
                if (!col.Contains(projectFile))
                {
                    col = col.Add(projectFile);
                }

                return col;
            });
        }

        public void ReportProjectNeedsRetargeting(string projectFile, IEnumerable<ProjectTargetChange> changes)
        {
            ThreadingTools.ApplyChangeOptimistically(ref _projectsToWaitFor, projectFile, (col, file) =>
            {
                if (col.Contains(file))
                {
                    col = col.Remove(file);
                }

                return col;
            });

            // fast path to not switch to the UI thread unnecessarily
            if (changes.Select(c => c.Description).Except(_registeredDescriptions).Any())
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    await _threadingService.SwitchToUIThread();

                    IVsTrackProjectRetargeting2 service = await _retargettingService.GetValueAsync();

                    foreach (ProjectTargetChange change in changes)
                    {
                        _needRetarget = true;

                        ErrorHandler.ThrowOnFailure(service.RegisterProjectTarget(change.Description));
                        ThreadingTools.ApplyChangeOptimistically(ref _registeredDescriptions, change, (col, c) => col.Add(c.Description));
                    }
                });
            }

            TryTriggerRetarget();
        }

        private void TryTriggerRetarget()
        {
            if (_projectsToWaitFor.Count == 0 && _needRetarget && _solutionOpened)
            {
                _ = _taskDelayScheduler.ScheduleAsyncTask(AllProjectsDoneLoading, default);
            }
        }

        public async Task AllProjectsDoneLoading(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();

            IVsTrackProjectRetargeting2 service = await _retargettingService.GetValueAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(service.CheckSolutionForRetarget((uint)__RETARGET_CHECK_OPTIONS.RCO_FIRST_SOLUTION_LOAD));
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            _solutionOpened = true;
            TryTriggerRetarget();
            return HResult.OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            _solutionOpened = false;
            return HResult.OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => HResult.OK;
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => HResult.OK;
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => HResult.OK;
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => HResult.OK;
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => HResult.OK;
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => HResult.OK;
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => HResult.OK;
        public int OnBeforeCloseSolution(object pUnkReserved) => HResult.OK;
    }
}
