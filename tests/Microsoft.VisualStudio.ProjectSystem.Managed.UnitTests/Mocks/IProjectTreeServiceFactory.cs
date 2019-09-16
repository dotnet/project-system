// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Moq;

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
            mock.SetupGet<IProjectTreeServiceState?>(s => s.CurrentTree)
                .Returns(action);

            return mock.Object;
        }
    }
}
