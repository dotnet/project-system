// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace NuGet.SolutionRestoreManager
{
    internal static class IVsSolutionRestoreService4Factory
    {
        public static IVsSolutionRestoreService4 Create()
        {
            return Mock.Of<IVsSolutionRestoreService4>();
        }

        internal static IVsSolutionRestoreService4 ImplementRegisterRestoreInfoSourceAsync(Action<IVsProjectRestoreInfoSource, CancellationToken>? registerAction = null)
        {
            var mock = new Mock<IVsSolutionRestoreService4>();

            if (registerAction is not null)
            {
                mock.Setup(s => s.RegisterRestoreInfoSourceAsync(It.IsAny<IVsProjectRestoreInfoSource>(), It.IsAny<CancellationToken>()))
                    .Callback(registerAction);
            }

            return mock.Object;
        }
    }
}
