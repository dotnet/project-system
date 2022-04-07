// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
