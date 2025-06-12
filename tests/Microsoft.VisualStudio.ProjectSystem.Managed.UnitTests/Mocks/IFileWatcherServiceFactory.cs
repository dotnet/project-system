// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Mocks;

internal static class IFileWatcherServiceFactory
{
    public static IFileWatcherService Create()
    {
        var fileWatcherService = new Mock<IFileWatcherService>();
        fileWatcherService.Setup(m => m.CreateFileWatcherAsync(It.IsAny<IFileWatcherServiceClient>(), It.IsAny<FileWatchChangeKinds>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IFileWatcher>(Mock.Of<IFileWatcher>()));

        return fileWatcherService.Object;
    }
}
