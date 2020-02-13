// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class IVsOnlineServicesFactory
    {
        public static IVsOnlineServices Create(bool online)
        {
            var mock = new Mock<IVsOnlineServices>();

            mock.SetupGet(s => s.ConnectedToVSOnline)
                .Returns(online);

            return mock.Object;
        }
    }
}
