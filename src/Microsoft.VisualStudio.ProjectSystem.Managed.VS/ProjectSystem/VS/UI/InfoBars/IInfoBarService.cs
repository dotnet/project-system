// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBars;

/// <summary>
/// Displays messages in the information bar attached to Visual Studio's main window.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.System)]
internal interface IInfoBarService
{
    /// <summary>
    /// Shows an information bar with the specified message, image and UI, replacing an existing one
    /// with the same message if there is one.
    /// </summary>
    Task ShowInfoBarAsync(string message, ImageMoniker image, CancellationToken cancellationToken, params ImmutableArray<InfoBarAction> actions);
}

/// <summary>
/// Represents an action that can be performed via info bars, such as a button or hyperlink.
/// </summary>
/// <param name="Title">Text to show in the button or hyperlink.</param>
/// <param name="Kind">Whether to show the action as a button or hyperlink.</param>
/// <param name="Callback">The callback to invoke when the user clicks this action.</param>
/// <param name="CloseAfterAction">Whether this action should also close the info bar after invoking the callback.</param>
internal sealed record class InfoBarAction(string Title, InfoBarActionKind Kind, Action Callback, bool CloseAfterAction);

/// <summary>
/// The presentation style of an <see cref="InfoBarAction"/>.
/// </summary>
internal enum InfoBarActionKind
{
    Button,
    Hyperlink
}
