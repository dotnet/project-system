// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IUserNotificationServices
    {
        Task<bool> CheckPromptAsync(string promptMessage);

        void NotifyFailureAsync(string renamedString);

        Task<bool> CheckPromptForRenameAsync(string promptMessage);

        void NotifyRenameFailureAsync(string failureMessage);
    }
}
