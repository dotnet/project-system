// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Shell;

[Export(typeof(IVsShellServices))]
[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
internal class VSShellServices : IVsShellServices
{

    private readonly AsyncLazy<(bool IsCommandLineMode, bool IsPopulateSolutionCacheMode)> _initialization;

    [ImportingConstructor]
    public VSShellServices(
        IVsUIService<SVsShell, IVsShell> vsShellService,
        IVsUIService<SVsAppCommandLine, IVsAppCommandLine> commandLineService,
        JoinableTaskContext joinableTaskContext)
    {
        _initialization = new(
            async () =>
            {
                // Initialisation must occur on the main thread.
                await joinableTaskContext.Factory.SwitchToMainThreadAsync();

                IVsShell? vsShell = vsShellService.Value;
                IVsAppCommandLine? commandLine = commandLineService.Value;

                bool isCommandLineMode = IsCommandLineMode();

                if (isCommandLineMode)
                {
                    return (IsCommandLineMode: true, IsPopulateSolutionCacheMode());
                }

                return (false, false);

                bool IsCommandLineMode()
                {
                    Assumes.Present(vsShell);

                    int hr = vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out object result);

                    return ErrorHandler.Succeeded(hr)
                        && result is bool isCommandLineMode
                        && isCommandLineMode;
                }

                bool IsPopulateSolutionCacheMode()
                {
                    if (commandLine is null)
                        return false;

                    int hr = commandLine.GetOption("populateSolutionCache", out int populateSolutionCache, out string commandValue);

                    return ErrorHandler.Succeeded(hr)
                        && Convert.ToBoolean(populateSolutionCache);
                }
            },
            joinableTaskContext.Factory);
    }

    public async Task<bool> IsCommandLineModeAsync(CancellationToken cancellationToken)
    {
        return (await _initialization.GetValueAsync(cancellationToken)).IsCommandLineMode;
    }

    public async Task<bool> IsPopulateSolutionCacheModeAsync(CancellationToken cancellationToken)
    {
        return (await _initialization.GetValueAsync(cancellationToken)).IsPopulateSolutionCacheMode;
    }
}
