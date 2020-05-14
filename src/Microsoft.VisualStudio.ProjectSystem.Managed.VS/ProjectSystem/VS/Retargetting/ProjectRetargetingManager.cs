// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectRetargetingManager))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class ProjectRetargetingManager : IProjectRetargetingManager
    {
        private List<string> _errors = new List<string>();
        private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _retargettingService;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public ProjectRetargetingManager(IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService,
                                         IProjectThreadingService threadingService)
        {
            _retargettingService = retargettingService;
            _threadingService = threadingService;
        }

        public Task ReportProjectNeedsRetargetingAsync(string projectFile, IEnumerable<IProjectTargetChange> changes)
        {
            _errors.Add(projectFile);

            return Task.CompletedTask;
        }

        public async Task AllProjectsDoneLoading()
        {
            await _threadingService.SwitchToUIThread();

            IVsTrackProjectRetargeting2 service = await _retargettingService.GetValueAsync();

            ErrorHandler.ThrowOnFailure(service.CheckSolutionForRetarget((uint)__RETARGET_CHECK_OPTIONS.RCO_FIRST_SOLUTION_LOAD));
        }
    }
}
