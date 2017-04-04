// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IDependenciesSnapshotProviderFactory
    {
        public static IDependenciesSnapshotProvider Create()
        {
            return Mock.Of<IDependenciesSnapshotProvider>();
        }

        public static IDependenciesSnapshotProvider Implement(
            IDependenciesSnapshot currentSnapshot = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IDependenciesSnapshotProvider>(behavior);            

            if (currentSnapshot != null)
            {
                mock.Setup(x => x.CurrentSnapshot).Returns(currentSnapshot);
            }

            return mock.Object;
        }
    }
}