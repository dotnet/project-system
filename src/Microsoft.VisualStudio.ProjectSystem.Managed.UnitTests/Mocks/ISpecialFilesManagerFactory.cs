// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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
