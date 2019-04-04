// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Moq;

namespace NuGet.SolutionRestoreManager
{
    internal static class IVsSolutionRestoreServiceFactory
    {
        public static IVsSolutionRestoreService Create()
        {
            return Mock.Of<IVsSolutionRestoreService>();
        }

        internal static IVsSolutionRestoreService ImplementNominateProjectAsync(Action<string, IVsProjectRestoreInfo, CancellationToken> action)
        {
            var mock = new Mock<IVsSolutionRestoreService>();
            mock.Setup(s => s.NominateProjectAsync(It.IsAny<string>(), It.IsAny<IVsProjectRestoreInfo>(), It.IsAny<CancellationToken>()))
                .Callback(action)
                .ReturnsAsync(true);

            return mock.Object;
        }
    }
}
