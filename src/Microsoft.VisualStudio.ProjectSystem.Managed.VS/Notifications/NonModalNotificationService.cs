// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;

namespace Microsoft.VisualStudio.Notifications;

/// <summary>
///     An implementation of <see cref="INonModalNotificationService"/> that delegates to the VS info bar.
/// </summary>
[Export(typeof(INonModalNotificationService))]
internal class NonModalNotificationService : INonModalNotificationService
{
    private readonly IInfoBarService _infoBarService;

    [ImportingConstructor]
    public NonModalNotificationService(IInfoBarService infoBarService)
    {
        _infoBarService = infoBarService;
    }

    public Task ShowMessageAsync(string message, CancellationToken cancellationToken)
    {
        return _infoBarService.ShowInfoBarAsync(message, KnownMonikers.StatusInformation, cancellationToken);
    }

    public Task ShowWarningAsync(string message, CancellationToken cancellationToken)
    {
        return _infoBarService.ShowInfoBarAsync(message, KnownMonikers.StatusWarning, cancellationToken);
    }

    public Task ShowErrorAsync(string message, CancellationToken cancellationToken)
    {
        return _infoBarService.ShowInfoBarAsync(message, KnownMonikers.StatusError, cancellationToken);
    }
}
