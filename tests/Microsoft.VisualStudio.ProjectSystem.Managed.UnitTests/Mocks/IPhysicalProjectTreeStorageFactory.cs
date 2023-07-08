// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPhysicalProjectTreeStorageFactory
    {
        public static IPhysicalProjectTreeStorage Create()
        {
            return Mock.Of<IPhysicalProjectTreeStorage>();
        }

        public static IPhysicalProjectTreeStorage ImplementCreateEmptyFileAsync(Action<string> action)
        {
            var mock = new Mock<IPhysicalProjectTreeStorage>();
            mock.Setup(p => p.CreateEmptyFileAsync(It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IPhysicalProjectTreeStorage ImplementAddFileAsync(Action<string> action)
        {
            var mock = new Mock<IPhysicalProjectTreeStorage>();
            mock.Setup(p => p.AddFileAsync(It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IPhysicalProjectTreeStorage ImplementAddFolderAsync(Action<string> action)
        {
            var mock = new Mock<IPhysicalProjectTreeStorage>();
            mock.Setup(p => p.AddFolderAsync(It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IPhysicalProjectTreeStorage ImplementCreateFolderAsync(Action<string> action)
        {
            var mock = new Mock<IPhysicalProjectTreeStorage>();
            mock.Setup(p => p.CreateFolderAsync(It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
