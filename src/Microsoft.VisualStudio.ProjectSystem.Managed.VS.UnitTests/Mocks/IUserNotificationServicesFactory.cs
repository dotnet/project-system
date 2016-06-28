// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IUserNotificationServicesFactory
    {
        public static IUserNotificationServices Create()
        {
            return Mock.Of<IUserNotificationServices>();
        }

        public static IUserNotificationServices Implement(bool confirmRename)
        {
            var mock = new Mock<IUserNotificationServices>();
           
            mock.Setup(h => h.Confirm(It.IsAny<string>()))
                .Returns(confirmRename);

            return mock.Object;
        }
    }
}
