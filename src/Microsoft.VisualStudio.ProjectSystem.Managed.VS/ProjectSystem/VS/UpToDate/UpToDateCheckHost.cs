// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.UpToDate;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    [Export(typeof(IUpToDateCheckHost))]
    internal sealed class UpToDateCheckHost : IUpToDateCheckHost
    {
        private readonly IVsUIService<SVsShell, IVsShell> _vsShell;
        private readonly JoinableTaskContext _joinableTaskContext;

        private bool? _hasDesignTimeBuild;

        [ImportingConstructor]
        public UpToDateCheckHost(IVsUIService<SVsShell, IVsShell> vsShell, JoinableTaskContext joinableTaskContext)
        {
            _vsShell = vsShell;
            _joinableTaskContext = joinableTaskContext;
        }

        public async ValueTask<bool> HasDesignTimeBuildsAsync(CancellationToken cancellationToken)
        {
            if (_hasDesignTimeBuild is null)
            {
                await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

                // If VS is running in command line mode, design-time builds do not occur
                if (ErrorHandler.Succeeded(_vsShell.Value.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out object resultObj)) && resultObj is bool result)
                {
                    _hasDesignTimeBuild = !result;
                }
                else
                {
                    _hasDesignTimeBuild = false;
                }
            }

            return _hasDesignTimeBuild.Value;
        }
    }
}
