// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class ITargetedDependenciesSnapshotFactory
    {
        public static ITargetedDependenciesSnapshot Create()
        {
            return Mock.Of<ITargetedDependenciesSnapshot>();
        }

        public static ITargetedDependenciesSnapshot Implement(
            string projectPath = null,
            ITargetFramework targetFramework = null,
            IEnumerable<IDependency> dependenciesWorld = null,
            bool? hasUnresolvedDependency = null,
            IProjectCatalogSnapshot catalogs = null,
            IEnumerable<IDependency> topLevelDependencies = null,
            bool? checkForUnresolvedDependencies = null,
            MockBehavior mockBehavior = MockBehavior.Default)
        {
            return ImplementMock(
                projectPath,
                targetFramework,
                dependenciesWorld,
                hasUnresolvedDependency,
                catalogs,
                topLevelDependencies,
                checkForUnresolvedDependencies,
                mockBehavior).Object;
        }

        public static Mock<ITargetedDependenciesSnapshot> ImplementMock(
            string projectPath = null,
            ITargetFramework targetFramework = null,
            IEnumerable<IDependency> dependenciesWorld = null,
            bool? hasUnresolvedDependency = null,
            IProjectCatalogSnapshot catalogs = null,
            IEnumerable<IDependency> topLevelDependencies = null,
            bool? checkForUnresolvedDependencies = null,
            MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<ITargetedDependenciesSnapshot>(mockBehavior);

            if (projectPath != null)
            {
                mock.Setup(x => x.ProjectPath).Returns(projectPath);
            }

            if (targetFramework != null)
            {
                mock.Setup(x => x.TargetFramework).Returns(targetFramework);
            }

            if (dependenciesWorld != null)
            {
                mock.Setup(x => x.DependenciesWorld)
                    .Returns(dependenciesWorld.ToImmutableDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase));
            }

            if (hasUnresolvedDependency.HasValue)
            {
                mock.Setup(x => x.HasUnresolvedDependency).Returns(hasUnresolvedDependency.Value);
            }

            if (catalogs != null)
            {
                mock.Setup(x => x.Catalogs).Returns(catalogs);
            }

            if (topLevelDependencies != null)
            {
                Assert.True(topLevelDependencies.All(d => d.TopLevel));

                mock.Setup(x => x.TopLevelDependencies)
                    .Returns(ImmutableArray.CreateRange(topLevelDependencies));
            }

            if (checkForUnresolvedDependencies.HasValue)
            {
                mock.Setup(x => x.CheckForUnresolvedDependencies(It.IsAny<string>())).Returns(checkForUnresolvedDependencies.Value);
                mock.Setup(x => x.CheckForUnresolvedDependencies(It.IsAny<IDependency>())).Returns(checkForUnresolvedDependencies.Value);
            }

            return mock;
        }

        public static ITargetedDependenciesSnapshot ImplementHasUnresolvedDependency(
            string id,
            bool hasUnresolvedDependency,
            MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<ITargetedDependenciesSnapshot>(mockBehavior);

            mock.Setup(x => x.CheckForUnresolvedDependencies(It.Is<IDependency>(y => y.Id.Equals(id, StringComparison.OrdinalIgnoreCase))))
                .Returns(hasUnresolvedDependency);

            return mock.Object;
        }
    }
}
