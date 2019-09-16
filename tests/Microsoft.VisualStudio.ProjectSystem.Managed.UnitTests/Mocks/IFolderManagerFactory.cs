// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;

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
