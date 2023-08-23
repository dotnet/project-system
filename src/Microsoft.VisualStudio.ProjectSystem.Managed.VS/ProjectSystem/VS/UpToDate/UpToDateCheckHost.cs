// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    [Export(typeof(IUpToDateCheckHost))]
    internal sealed class UpToDateCheckHost : IUpToDateCheckHost
    {
        private readonly IVsUIService<IVsShell> _vsShell;
        private readonly IVsUIService<IVsAppCommandLine> _vsAppCommandLine;
        private readonly JoinableTaskContext _joinableTaskContext;

        private bool? _hasDesignTimeBuild;

        [ImportingConstructor]
        public UpToDateCheckHost(IVsUIService<SVsShell, IVsShell> vsShell, IVsUIService<SVsAppCommandLine, IVsAppCommandLine> vsAppCommandLine, JoinableTaskContext joinableTaskContext)
        {
            _vsShell = vsShell;
            _vsAppCommandLine = vsAppCommandLine;
            _joinableTaskContext = joinableTaskContext;
        }

        public async ValueTask<bool> HasDesignTimeBuildsAsync(CancellationToken cancellationToken)
        {
            _hasDesignTimeBuild ??= await ComputeValueAsync();

            return _hasDesignTimeBuild.Value;

            async ValueTask<bool> ComputeValueAsync()
            {
                await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

                if (ErrorHandler.Succeeded(_vsAppCommandLine.Value.GetOption("populateSolutionCache", out int populateSolutionCachePresent, out string _)))
                {
                    if (populateSolutionCachePresent != 0)
                    {
                        // Design time builds are available when running with /populateSolutionCache.
                        return true;
                    }
                }

                if (ErrorHandler.Succeeded(_vsShell.Value.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out object value)))
                {
                    if (value is bool isInCommandLineMode)
                    {
                        // Design-time builds do not occur in command line mode, other than with /populateSolutionCache (checked earlier).
                        return !isInCommandLineMode;
                    }
                }

                // We shouldn't reach this point.
                System.Diagnostics.Debug.Fail($"{nameof(UpToDateCheckHost)}.{nameof(HasDesignTimeBuildsAsync)} was unable to determine result reliably.");

                // Assume we don't have design-time builds, to prevent hangs from waiting for snapshot data that will never arrive.
                return false;
            }
        }
    }
}
