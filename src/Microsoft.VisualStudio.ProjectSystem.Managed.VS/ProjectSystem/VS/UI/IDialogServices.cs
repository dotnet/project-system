// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Provides an abstraction over dialogs to make them unit testable. Each dialog will have its own abstraction which
    /// can be retrieved from this service.
    /// </summary>
    internal interface IDialogServices
    {
        bool DontShowAgainMessageBox(string caption, string message, string? checkboxText, bool initialStateOfCheckbox, string learnMoreText, string learnMoreUrl);
    }
}
