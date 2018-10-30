// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsFileChangeExFactory
    {
        public static IVsAsyncFileChangeEx CreateWithAdviseUnadviseFileChange(uint adviseCookie)
        {
            var mock = new Mock<IVsAsyncFileChangeEx>();
            mock.Setup(x => x.AdviseFileChangeAsync(It.IsAny<string>(), It.IsAny<_VSFILECHANGEFLAGS>(), It.IsAny<IVsFreeThreadedFileChangeEvents2>(), It.IsAny<CancellationToken>())).ReturnsAsync(adviseCookie);
            mock.Setup(x => x.UnadviseFileChangeAsync(It.IsAny<uint>(), It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
            return mock.Object;
        }
    }
}
