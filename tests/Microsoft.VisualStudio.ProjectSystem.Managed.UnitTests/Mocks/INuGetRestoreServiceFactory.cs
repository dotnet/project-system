// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

internal static class INuGetRestoreServiceFactory
{
    public static INuGetRestoreService Create()
    {
        return Mock.Of<INuGetRestoreService>();
    }

    internal static INuGetRestoreService ImplementNominateProjectAsync(Action<ProjectRestoreInfo, IReadOnlyCollection<PackageRestoreConfiguredInput>, CancellationToken> action)
    {
        var mock = new Mock<INuGetRestoreService>();
        mock.Setup(s => s.NominateAsync(It.IsAny<ProjectRestoreInfo>(), It.IsAny<IReadOnlyCollection<PackageRestoreConfiguredInput>>(), It.IsAny<CancellationToken>()))
            .Callback(action)
            .ReturnsAsync(true);

        return mock.Object;
    }
}
