// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;

/// <summary>
/// Represents the kind of a <see cref="InfoBarUI"/>.
/// </summary>
internal enum InfoBarUIKind
{
    /// <summary>
    /// The <see cref="InfoBarUI"/> is a button.
    /// </summary>
    Button,

    /// <summary>
    /// The <see cref="InfoBarUI"/> is a hyperlink.
    /// </summary>
    Hyperlink,
}
