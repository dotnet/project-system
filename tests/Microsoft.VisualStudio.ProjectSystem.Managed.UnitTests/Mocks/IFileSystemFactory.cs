// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.IO
{
    internal static class IFileSystemFactory
    {
        public static IFileSystem ImplementTryGetLastFileWriteTimeUtc(FuncWithOut<string, DateTime?, bool> action)
        {
            DateTime? result;
            var mock = new Mock<IFileSystem>();
            mock.Setup(f => f.TryGetLastFileWriteTimeUtc(It.IsAny<string>(), out result))
                .Returns(action);

            return mock.Object;
        }

        public static IFileSystem Create()
        {
            return Mock.Of<IFileSystem>();
        }

        public static IFileSystem ImplementCreate(Action<string> action)
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(f => f.Create(It.IsAny<string>()))
                .Callback(action);

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
            Func<string, string>? readAllTextFunc = null)
        {
            var mock = new Mock<IFileSystem>();

            mock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(existsFunc);

            if (readAllTextFunc is not null)
            {
                mock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>()))
                    .Returns((Func<string, Task<string>>)(path => Task.FromResult(readAllTextFunc(path))));
            }

            return mock.Object;
        }
    }
}
