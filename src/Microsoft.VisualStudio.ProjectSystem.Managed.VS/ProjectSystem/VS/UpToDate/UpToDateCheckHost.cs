// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.AppId.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    [Export(typeof(IUpToDateCheckHost))]
    internal sealed class UpToDateCheckHost : IUpToDateCheckHost
    {
        private readonly IVsService<IVsAppId, IVsAppId> _vsAppId;
        private readonly IVsService<IVsAppCommandLine> _vsAppCommandLine;
        private readonly JoinableTaskContext _joinableTaskContext;

        private bool? _hasDesignTimeBuild;

        [ImportingConstructor]
        public UpToDateCheckHost(
            IVsService<IVsAppId, IVsAppId> vsAppId,
            IVsService<SVsAppCommandLine, IVsAppCommandLine> vsAppCommandLine,
            JoinableTaskContext joinableTaskContext)
        {
            _vsAppId = vsAppId;
            _vsAppCommandLine = vsAppCommandLine;
            _joinableTaskContext = joinableTaskContext;
        }

        public async ValueTask<bool> HasDesignTimeBuildsAsync(CancellationToken cancellationToken)
        {
            _hasDesignTimeBuild ??= await HasDesignTimeBuildsInternalAsync();

            return _hasDesignTimeBuild.Value;

            async Task<bool> HasDesignTimeBuildsInternalAsync()
            {
                IVsAppCommandLine vsAppCommandLine = await _vsAppCommandLine.GetValueAsync(cancellationToken);

                if (ErrorHandler.Succeeded(vsAppCommandLine.GetOption("populateSolutionCache", out int populateSolutionCachePresent, out string _)))
                {
                    if (populateSolutionCachePresent != 0)
                    {
                        // Design time builds are available when running with /populateSolutionCache.
                        return true;
                    }
                }

                IVsAppId vsAppId = await _vsAppId.GetValueAsync(cancellationToken);

                if (ErrorHandler.Succeeded(vsAppId.GetProperty((int)__VSAPROPID10.VSAPROPID_IsInCommandLineMode, out object value)))
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
