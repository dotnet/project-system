// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    internal static class ISpecialFilesManagerFactory
    {
        public static ISpecialFilesManager ImplementGetFile(string? result)
        {
            var mock = new Mock<ISpecialFilesManager>();
            mock.Setup(m => m.GetFileAsync(It.IsAny<SpecialFiles>(), It.IsAny<SpecialFileFlags>()))
                .ReturnsAsync(result);

            return mock.Object;
        }

        public static ISpecialFilesManager Create()
        {
            return Mock.Of<ISpecialFilesManager>();
        }
    }
}
