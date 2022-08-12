// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;

/// <summary>
/// Displays messages in the information bar attached to Visual Studio's main window.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System)]
internal interface IInfoBarService
{
    /// <summary>
    /// Shows an information bar with the specified message, image and UI, replacing an existing one
    /// with the same message if there is one.
    /// </summary>
    Task ShowInfoBarAsync(string message, ImageMoniker image, CancellationToken cancellationToken, params InfoBarUI[] items);
}
