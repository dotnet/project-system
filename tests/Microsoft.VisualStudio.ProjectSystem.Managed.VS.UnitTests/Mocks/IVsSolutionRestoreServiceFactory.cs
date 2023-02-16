// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;

namespace NuGet.SolutionRestoreManager
{
    internal static class IVsSolutionRestoreServiceFactory
    {
        public static IVsSolutionRestoreService3 Create()
        {
            return Mock.Of<IVsSolutionRestoreService3>();
        }

        internal static IVsSolutionRestoreService3 ImplementNominateProjectAsync(Action<string, IVsProjectRestoreInfo2, CancellationToken> action)
        {
            var mock = new Mock<IVsSolutionRestoreService3>();
            mock.Setup(s => s.NominateProjectAsync(It.IsAny<string>(), It.IsAny<IVsProjectRestoreInfo2>(), It.IsAny<CancellationToken>()))
                .Callback(action)
                .ReturnsAsync(true);

            return mock.Object;
        }
    }
}

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{ 
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
}
