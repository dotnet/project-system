// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IUserNotificationServices
    {
        bool Confirm(string message);

        void ShowWarning(string warning);

        /// <summary>
        /// Note that typically you wan to set the title to null and have VS decide on the caption. If you do add a title it will
        /// appear in the message box area above the message string
        /// </summary>
        void ShowError(string error);
    }
}
