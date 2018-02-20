// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IAggregateDependenciesSnapshotProviderFactory
    {
        public static IAggregateDependenciesSnapshotProvider Create()
        {
            return Mock.Of<IAggregateDependenciesSnapshotProvider>();
        }

        public static IAggregateDependenciesSnapshotProvider Implement(
            IDependenciesSnapshotProvider getSnapshotProvider = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IAggregateDependenciesSnapshotProvider>(behavior);            

            if (getSnapshotProvider != null)
            {
                mock.Setup(x => x.GetSnapshotProvider(It.IsAny<string>()))
                    .Returns(getSnapshotProvider);
            }

            return mock.Object;
        }
    }
}
