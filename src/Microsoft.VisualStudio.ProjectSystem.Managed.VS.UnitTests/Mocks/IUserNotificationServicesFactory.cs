// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IUserNotificationServicesFactory
    {
        public static IUserNotificationServices Implement(Func<string, Task<bool>> promptForRename)
        {
            var mock = new Mock<IUserNotificationServices>();
           
            mock.Setup(h => h.CheckPromptForRenameAsync(It.IsAny<string>()))
                .Returns(promptForRename);

            return mock.Object;
        }
    }
}
