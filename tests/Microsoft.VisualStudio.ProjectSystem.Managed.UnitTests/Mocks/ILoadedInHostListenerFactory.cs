// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ILoadedInHostListenerFactory
    {
        public static ILoadedInHostListener Create()
        {
            return Mock.Of<ILoadedInHostListener>();
        }

        public static ILoadedInHostListener ImplementStartListeningAsync(Action action)
        {
            var mock = new Mock<ILoadedInHostListener>();
            mock.Setup(t => t.StartListeningAsync())
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
