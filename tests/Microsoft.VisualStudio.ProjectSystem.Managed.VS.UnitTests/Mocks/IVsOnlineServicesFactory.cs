// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

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
