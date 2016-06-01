// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class IFileSystemFactory
    {
        public static IFileSystem CreateWithExists(Func<string, bool> existsFunc)
        {
            var mock = new Mock<IFileSystem>();

            mock.Setup(f => f.Exists(It.IsAny<string>())).Returns(existsFunc);

            return mock.Object;
        }
    }
}
