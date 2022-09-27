// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;

/// <summary>
/// Represents information bar user interface elements.
/// </summary>
internal class InfoBarUI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfoBarUI"/> class.
    /// </summary>
    /// <param name="title">The title of the UI _element.</param>
    /// <param name="kind">The kind of the UI _element, either <see cref="InfoBarUIKind.Button"/> or <see cref="InfoBarUIKind.Hyperlink"/></param>
    /// <param name="action">The <see cref="Action"/> to run when the UI _element is clicked</param>
    /// <param name="closeAfterAction"><see langword="true"/> if the information bar should close after <paramref name="action"/> is run.</param>
    public InfoBarUI(string title, InfoBarUIKind kind, Action action, bool closeAfterAction = true)
    {
        Requires.NotNullOrEmpty(title, nameof(title));
        Requires.NotNull(action, nameof(action));

        Title = title;
        Kind = kind;
        Action = action;
        CloseAfterAction = closeAfterAction;
    }

    /// <summary>
    /// Gets the title of the UI _element.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the kind of the UI _element, either <see cref="InfoBarUIKind.Button"/> or <see cref="InfoBarUIKind.Hyperlink"/>.
    /// </summary>
    public InfoBarUIKind Kind { get; }

    /// <summary>
    /// Gets the <see cref="Action"/> to run when the UI _element is clicked.
    /// </summary>
    public Action Action { get; }

    /// <summary>
    /// Gets whether information bar should close after <see cref="Action"/> is run.
    /// </summary>
    public bool CloseAfterAction { get; }
}
