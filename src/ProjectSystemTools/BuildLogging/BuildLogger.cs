// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal sealed class BuildLogger : IVsUpdateSolutionEvents4, IDisposable
    {
        private IVsSolutionBuildManager5 _updateSolutionEventsService;
        private readonly uint _updateSolutionEventsCookie;
        private bool _isDisposed;

        public bool IsLogging { get; private set; }

        public BuildLogger(IServiceProvider serviceProvider)
        {
            _updateSolutionEventsService = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager5;
            _updateSolutionEventsService?.AdviseUpdateSolutionEvents4(this, out _updateSolutionEventsCookie);
        }

        public void Start() => IsLogging = true;

        public void Stop() => IsLogging = false;

        public event EventHandler<BuildOperation> BuildStarted;

        public event EventHandler<BuildOperation> BuildEnded;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_updateSolutionEventsService != null)
                {
                    _updateSolutionEventsService.UnadviseUpdateSolutionEvents4(_updateSolutionEventsCookie);
                    _updateSolutionEventsService = null;
                }
            }

            _isDisposed = true;
        }

        private static BuildOperation ActionToOperation(uint dwAction)
        {
            var action = (VSSOLNBUILDUPDATEFLAGS)dwAction;

            switch (action & VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_MASK)
            {
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN:
                    return BuildOperation.Clean;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD:
                    return BuildOperation.Build;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE:
                    return BuildOperation.Rebuild;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_DEPLOY:
                    return BuildOperation.Deploy;
            }

            var action2 = (VSSOLNBUILDUPDATEFLAGS2)dwAction;

            switch (action2 & (VSSOLNBUILDUPDATEFLAGS2)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_MASK)
            {
                case VSSOLNBUILDUPDATEFLAGS2.SBF_OPERATION_PUBLISH:
                    return BuildOperation.Publish;
                case VSSOLNBUILDUPDATEFLAGS2.SBF_OPERATION_PUBLISHUI:
                    return BuildOperation.PublishUI;
                default:
                    return BuildOperation.Unknown;
            }
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_QueryDelayFirstUpdateAction(out int pfDelay) => pfDelay = 0;

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginFirstUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_EndLastUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginUpdateAction(uint dwAction) => BuildStarted?.Invoke(this, ActionToOperation(dwAction));

        void IVsUpdateSolutionEvents4.UpdateSolution_EndUpdateAction(uint dwAction) => BuildEnded?.Invoke(this, ActionToOperation(dwAction));

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchBegin()
        {
        }

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchEnd()
        {
        }
    }
}