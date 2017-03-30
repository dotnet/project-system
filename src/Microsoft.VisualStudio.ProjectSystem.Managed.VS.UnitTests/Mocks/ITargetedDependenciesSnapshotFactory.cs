// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class ITargetedDependenciesSnapshotFactory
    {
        public static ITargetedDependenciesSnapshot Create()
        {
            return Mock.Of<ITargetedDependenciesSnapshot>();
        }

        public static ITargetedDependenciesSnapshot Implement(
            ITargetFramework targetFramework = null,
            Dictionary<string, IDependency> dependenciesWorld = null,
            MockBehavior? mockBehavior = null)
        {
            return ImplementMock(targetFramework, dependenciesWorld, mockBehavior).Object;
        }

        public static Mock<ITargetedDependenciesSnapshot> ImplementMock(
            ITargetFramework targetFramework = null,
            Dictionary<string, IDependency> dependenciesWorld = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<ITargetedDependenciesSnapshot>(behavior);

            if (targetFramework != null)
            {
                mock.Setup(x => x.TargetFramework).Returns(targetFramework);
            }

            if (dependenciesWorld != null)
            {
                mock.Setup(x => x.DependenciesWorld)
                    .Returns(ImmutableDictionary<string, IDependency>.Empty.AddRange(dependenciesWorld));
            }

            return mock;
        }

        public static ITargetedDependenciesSnapshot ImplementHasUnresolvedDependency(
            string id,
            bool hasUnresolvedDependency,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<ITargetedDependenciesSnapshot>(behavior);

            mock.Setup(x => x.CheckForUnresolvedDependencies(It.Is<IDependency>(y => y.Id.Equals(id))))
                .Returns(hasUnresolvedDependency);

            return mock.Object;
        }
    }
}