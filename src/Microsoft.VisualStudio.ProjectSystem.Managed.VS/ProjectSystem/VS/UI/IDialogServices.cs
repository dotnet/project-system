// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Provides an abstraction over dialogs to make them unit testable. Each dialog will have its own abstraction which
    /// can be retrieved from this servcie. 
    /// </summary>
    internal interface IDialogServices
    {
        MultiChoiceMsgBoxResult ShowMultiChoiceMsgBox(string dialogTitle, string errorText, string[] buttons);
        bool DontShowAgainMessageBox(string caption, string message, string checkboxText, bool initialStateOfCheckbox, string learnMoreText, string learnMoreUrl);
    }
}
