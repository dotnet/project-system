// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectSystemOptionsWithChangesFactory
    {
        public static IProjectSystemOptionsWithChanges Create()
        {
            return Mock.Of<IProjectSystemOptionsWithChanges>();
        }

        public static IProjectSystemOptionsWithChanges ImplementGetSkipAnalyzersForImplicitlyTriggeredBuildAsync(Func<CancellationToken, bool> result)
        {
            var mock = new Mock<IProjectSystemOptionsWithChanges>();
            mock.Setup(o => o.GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }
    }
}
