// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Shell;

[Export(typeof(IVsShellServices))]
[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
internal class VSShellServices : IVsShellServices
{
    private static readonly object s_lockObject = new();
    private static Task<bool>? s_isCommandLine;
    private static Task<bool>? s_isPopulateCache;

    [ImportingConstructor]
    public VSShellServices(
        IVsService<IVsAppId> vsAppId,
        IVsService<SVsAppCommandLine, IVsAppCommandLine> commandLine)
    {
        lock (s_lockObject)
        {
            s_isCommandLine ??= IsCommandLineModeAsync(vsAppId);
            s_isPopulateCache ??= IsPopulateSolutionCacheModeAsync(commandLine);

        }

        static async Task<bool> IsCommandLineModeAsync(IVsService<IVsAppId> vsAppIdService)
        {
            IVsAppId? vsAppId = await vsAppIdService.GetValueOrNullAsync();
            if (vsAppId is not null)
            {
                const int VSAPROPID_IsInCommandLineMode = (int)Microsoft.Internal.VisualStudio.AppId.Interop.__VSAPROPID10.VSAPROPID_IsInCommandLineMode;
                return ErrorHandler.Succeeded(vsAppId.GetProperty(VSAPROPID_IsInCommandLineMode, out object o)) && o is bool isInCommandLineMode && isInCommandLineMode;
            }

            return false;
        }

        static async Task<bool> IsPopulateSolutionCacheModeAsync(IVsService<SVsAppCommandLine, IVsAppCommandLine> commandLineService)
        {
            IVsAppCommandLine? commandLine = await commandLineService.GetValueOrNullAsync();
            if (commandLine is null)
                return false;

            int hr = commandLine.GetOption("populateSolutionCache", out int populateSolutionCache, out string _);

            return ErrorHandler.Succeeded(hr)
                && Convert.ToBoolean(populateSolutionCache);
        }
    }

    public async Task<bool> IsCommandLineModeAsync(CancellationToken cancellationToken)
    {
        return s_isCommandLine is not null && await s_isCommandLine;
    }

    public async Task<bool> IsPopulateSolutionCacheModeAsync(CancellationToken cancellationToken)
    {
        return s_isPopulateCache is not null && await s_isPopulateCache;
    }
}
