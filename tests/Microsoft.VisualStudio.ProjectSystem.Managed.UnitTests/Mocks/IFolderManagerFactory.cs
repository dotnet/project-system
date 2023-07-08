// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IFolderManagerFactory
    {
        public static IFolderManager Create()
        {
            return Mock.Of<IFolderManager>();
        }

        public static IFolderManager IncludeFolderInProjectAsync(Action<string, bool> action)
        {
            var mock = new Mock<IFolderManager>();

            mock.Setup(m => m.IncludeFolderInProjectAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IFolderManager IncludeFolderInProjectAsync(Func<string, bool, Task> action)
        {
            var mock = new Mock<IFolderManager>();

            mock.Setup(m => m.IncludeFolderInProjectAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
