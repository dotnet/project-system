// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IUserNotificationServices
    {
        bool Confirm(string message);

        void NotifyFailure(string failureMessage);

        void ReportErrorInfo(int hr);
    }
}
