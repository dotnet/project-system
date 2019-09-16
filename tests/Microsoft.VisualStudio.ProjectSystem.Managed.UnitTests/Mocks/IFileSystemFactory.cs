// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Moq;

namespace Microsoft.VisualStudio.IO
{
    internal static class IFileSystemFactory
    {
        public static IFileSystem Create()
        {
            return Mock.Of<IFileSystem>();
        }

        public static IFileSystem ImplementCreate(Func<string, Stream> action)
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(f => f.Create(It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }

        public static IFileSystem ImplementCreateDirectory(Action<string> action)
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(f => f.CreateDirectory(It.IsAny<string>()))
                .Callback(action);

            return mock.Object;
        }

        public static IFileSystem Create(
            Func<string, bool> existsFunc,
            Func<string, FileStream>? createFunc = null,
            Func<string, string>? readAllTextFunc = null)
        {
            var mock = new Mock<IFileSystem>();

            mock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(existsFunc);
            mock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(readAllTextFunc);

            if (createFunc != null)
            {
                mock.Setup(f => f.Create(It.IsAny<string>())).Returns(createFunc);
            }

            return mock.Object;
        }
    }
}
