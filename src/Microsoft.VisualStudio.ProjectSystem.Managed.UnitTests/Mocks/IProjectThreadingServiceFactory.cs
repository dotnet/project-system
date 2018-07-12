// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static partial class IProjectThreadingServiceFactory
    {
        public static IProjectThreadingService Create()
        {
            return new ProjectThreadingService();
        }

        public static IProjectThreadingService ImplementVerifyOnUIThread(Action action)
        {
            var mock = new Mock<IProjectThreadingService>();

            mock.Setup(s => s.VerifyOnUIThread())
                .Callback(action);

            return mock.Object;
        }
    }
}
