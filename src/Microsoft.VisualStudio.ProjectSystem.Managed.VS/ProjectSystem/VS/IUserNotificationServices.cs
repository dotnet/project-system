// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IUserNotificationServices
    {
        bool Confirm(string message);

        void NotifyFailure(string failureMessage);

        void ReportErrorInfo(int hr);

        /// <summary>
        /// Note that typically you wan to set the title to null and have VS decide on the caption. If you do add a title it will
        /// appear in the message box area above the message string
        /// </summary>
        int ShowMessageBox(string message, string title, OLEMSGICON icon, OLEMSGBUTTON msgButton, OLEMSGDEFBUTTON defaultButton);
    }
}
