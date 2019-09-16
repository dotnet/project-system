// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Moq;

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
