// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectCapabilitiesServiceFactory
    {
        public static IProjectCapabilitiesService Create()
        {
            return Mock.Of<IProjectCapabilitiesService>();
        }

        public static IProjectCapabilitiesService ImplementsContains(Func<string, bool> action)
        {
            var mock = new Mock<IProjectCapabilitiesService>();

            mock.Setup(s => s.Contains(It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
