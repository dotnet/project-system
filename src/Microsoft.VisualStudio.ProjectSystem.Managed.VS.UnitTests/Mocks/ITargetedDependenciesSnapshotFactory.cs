// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
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
            bool? hasUnresolvedDependency = null,
            IProjectCatalogSnapshot catalogs = null,
            IEnumerable<IDependency> topLevelDependencies = null,
            bool? checkForUnresolvedDependencies = null,
            MockBehavior? mockBehavior = null)
        {
            return ImplementMock(
                targetFramework,
                dependenciesWorld,
                hasUnresolvedDependency,
                catalogs,
                topLevelDependencies,
                checkForUnresolvedDependencies,
                mockBehavior).Object;
        }

        public static Mock<ITargetedDependenciesSnapshot> ImplementMock(
            ITargetFramework targetFramework = null,
            Dictionary<string, IDependency> dependenciesWorld = null,
            bool? hasUnresolvedDependency = null,
            IProjectCatalogSnapshot catalogs = null,
            IEnumerable<IDependency> topLevelDependencies = null,
            bool? checkForUnresolvedDependencies = null,
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
                    .Returns(ImmutableDictionary<string, IDependency>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase).AddRange(dependenciesWorld));
            }

            if (hasUnresolvedDependency != null && hasUnresolvedDependency.HasValue)
            {
                mock.Setup(x => x.HasUnresolvedDependency).Returns(hasUnresolvedDependency.Value);
            }

            if (catalogs != null)
            {
                mock.Setup(x => x.Catalogs).Returns(catalogs);
            }

            if (topLevelDependencies != null)
            {
                var dependencies = ImmutableHashSet<IDependency>.Empty;
                foreach (var d in topLevelDependencies)
                {
                    dependencies = dependencies.Add(d);
                }

                mock.Setup(x => x.TopLevelDependencies).Returns(dependencies);
            }

            if (checkForUnresolvedDependencies != null && checkForUnresolvedDependencies.HasValue)
            {
                mock.Setup(x => x.CheckForUnresolvedDependencies(It.IsAny<string>())).Returns(checkForUnresolvedDependencies.Value);
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

            mock.Setup(x => x.CheckForUnresolvedDependencies(It.Is<IDependency>(y => y.Id.Equals(id, System.StringComparison.OrdinalIgnoreCase))))
                .Returns(hasUnresolvedDependency);

            return mock.Object;
        }
    }
}
