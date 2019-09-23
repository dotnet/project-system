// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class DependenciesSnapshotFactory
    {
        public static DependenciesSnapshot Implement(
            Dictionary<ITargetFramework, TargetedDependenciesSnapshot>? dependenciesByTarget = null,
            bool? hasUnresolvedDependency = null,
            ITargetFramework? activeTarget = null,
            MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<DependenciesSnapshot>(mockBehavior);

            if (dependenciesByTarget != null)
            {
                mock.Setup(x => x.DependenciesByTargetFramework).Returns(dependenciesByTarget.ToImmutableDictionary());
            }

            if (hasUnresolvedDependency.HasValue)
            {
                mock.Setup(x => x.HasReachableVisibleUnresolvedDependency).Returns(hasUnresolvedDependency.Value);
            }

            if (activeTarget != null)
            {
                mock.Setup(x => x.ActiveTargetFramework).Returns(activeTarget);
            }

            return mock.Object;
        }
    }
}
