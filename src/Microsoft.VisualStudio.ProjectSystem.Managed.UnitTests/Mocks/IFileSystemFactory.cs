// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class IFileSystemFactory
    {
        public static IFileSystem Create(Func<string, bool> existsFunc, Func<string, FileStream> createFunc = null)
        {
            var mock = new Mock<IFileSystem>();

            mock.Setup(f => f.Exists(It.IsAny<string>())).Returns(existsFunc);

            if (createFunc != null)
            {
                mock.Setup(f => f.Create(It.IsAny<string>())).Returns(createFunc);
            }

            return mock.Object;
        }
    }
}
