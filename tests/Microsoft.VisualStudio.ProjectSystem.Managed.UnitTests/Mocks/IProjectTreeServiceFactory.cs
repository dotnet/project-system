// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeServiceFactory
    {
        public static IProjectTreeService Create(IProjectTree? tree = null, IProjectTreeProvider? treeProvider = null)
        {
            var mock = new Mock<IProjectTreeService>();

            var treeState = IProjectTreeServiceStateFactory.ImplementTree(() => tree, () => treeProvider ?? IProjectTreeProviderFactory.Create());

            mock.Setup(s => s.PublishAnyNonLoadingTreeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(treeState);

            mock.Setup(s => s.PublishAnyNonNullTreeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(treeState);

            mock.Setup(s => s.PublishLatestTreeAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(treeState);

            mock.SetupGet(s => s.CurrentTree)
                .Returns(treeState);

            return mock.Object;
        }

        public static IProjectTreeService Create()
        {
            return Mock.Of<IProjectTreeService>();
        }

        public static IProjectTreeService ImplementCurrentTree(Func<IProjectTreeServiceState?> action)
        {
            var mock = new Mock<IProjectTreeService>();
            mock.SetupGet(s => s.CurrentTree)
                .Returns(action);

            return mock.Object;
        }
    }
}
