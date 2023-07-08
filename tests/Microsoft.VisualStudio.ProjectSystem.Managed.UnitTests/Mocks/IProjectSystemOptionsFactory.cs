// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSystemOptionsFactory
    {
        public static IProjectSystemOptions Create()
        {
            return Mock.Of<IProjectSystemOptions>();
        }

        public static IProjectSystemOptions ImplementGetUseDesignerByDefaultAsync(Func<string, bool, CancellationToken, bool> result)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.Setup(o => o.GetUseDesignerByDefaultAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }

        public static IProjectSystemOptions ImplementSetUseDesignerByDefaultAsync(Func<string, bool, CancellationToken, Task> result)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.Setup(o => o.SetUseDesignerByDefaultAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(result);

            return mock.Object;
        }

        public static IProjectSystemOptions ImplementGetSkipAnalyzersForImplicitlyTriggeredBuildAsync(Func<CancellationToken, bool> result)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.Setup(o => o.GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }

        public static IProjectSystemOptions ImplementGetPreferSingleTargetBuildsForStartupProjectsAsync(Func<CancellationToken, bool> result)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.Setup(o => o.GetPreferSingleTargetBuildsForStartupProjectsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }
    }
}
