// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices;

[Export(typeof(ILanguageServiceHostEnvironment))]
internal sealed class LanguageServiceHostEnvironment : ILanguageServiceHostEnvironment
{
    private readonly AsyncLazy<bool> _isEnabled;

    [ImportingConstructor]
    public LanguageServiceHostEnvironment(IVsUIService<SVsShell, IVsShell> vsShell, JoinableTaskContext joinableTaskContext)
    {
        _isEnabled = new(
            async () =>
            {
                await joinableTaskContext.Factory.SwitchToMainThreadAsync();

                // If VS is running in command line mode (e.g. "devenv.exe /build my.sln"),
                // the language service host is not enabled.

                return ErrorHandler.Succeeded(vsShell.Value.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out object resultObj))
                    && resultObj is bool result
                    && !result; // negate: enabled when not in command line mode
            },
            joinableTaskContext.Factory);
    }

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken) => _isEnabled.GetValueAsync(cancellationToken);
}
