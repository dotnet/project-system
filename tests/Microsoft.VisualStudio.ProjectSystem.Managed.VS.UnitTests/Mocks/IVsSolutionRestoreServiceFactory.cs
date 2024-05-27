// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace NuGet.SolutionRestoreManager
{
    internal class IVsSolutionRestoreServiceFactory
    {
        private readonly Mock<IVsSolutionRestoreService5> _mock = new();

        internal IVsSolutionRestoreServiceFactory WithNominateProjectAsync(Action<string, IVsProjectRestoreInfo3, CancellationToken> action)
        {
            _mock.Setup(s => s.NominateProjectAsync(It.IsAny<string>(), It.IsAny<IVsProjectRestoreInfo3>(), It.IsAny<CancellationToken>()))
                 .Callback(action)
                 .ReturnsAsync(true);

            return this;
        }

        internal IVsSolutionRestoreServiceFactory WithRegisterRestoreInfoSourceAsync(Action<IVsProjectRestoreInfoSource, CancellationToken>? registerAction = null)
        {
            if (registerAction is not null)
            {
                _mock.Setup(s => s.RegisterRestoreInfoSourceAsync(It.IsAny<IVsProjectRestoreInfoSource>(), It.IsAny<CancellationToken>()))
                     .Callback(registerAction);
            }

            return this;
        }

        internal IVsSolutionRestoreService5 Build()
        {
            return _mock.Object;
        }
    }
}
