// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices;

[Export(typeof(ILanguageServiceHostEnvironment))]
[AppliesTo(ProjectCapability.DotNetLanguageService)]
internal sealed class LanguageServiceHostEnvironment : ILanguageServiceHostEnvironment
{
    private readonly AsyncLazy<bool> _isEnabled;

    [ImportingConstructor]
    public LanguageServiceHostEnvironment(IVsShellServices vsShell, JoinableTaskContext joinableTaskContext)
    {
        _isEnabled = new(
            async () =>
            {
                await joinableTaskContext.Factory.SwitchToMainThreadAsync();

                // If VS is running in command line mode (e.g. "devenv.exe /build my.sln"),
                // the language service host is not enabled. The one exception to this is
                // when we're populating a solution cache via "/populateSolutionCache".
                return !await vsShell.IsCommandLineModeAsync()
                    || await vsShell.IsPopulateSolutionCacheModeAsync();
            },
            joinableTaskContext.Factory);
    }

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken) => _isEnabled.GetValueAsync(cancellationToken);
}
