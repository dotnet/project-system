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

        void IVsUpdateSolutionEvents4.UpdateSolution_QueryDelayFirstUpdateAction(out int pfDelay)
        {
            pfDelay = 0;
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginFirstUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_EndLastUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginUpdateAction(uint dwAction)
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_EndUpdateAction(uint dwAction)
        {
        }

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchBegin()
        {
        }

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchEnd()
        {
        }
    }
}