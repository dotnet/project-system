// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IDependenciesSnapshotFactory
    {
        public static IDependenciesSnapshot Create()
        {
            return Mock.Of<IDependenciesSnapshot>();
        }

        public static IDependenciesSnapshot Implement(
            Dictionary<ITargetFramework, ITargetedDependenciesSnapshot> targets = null,
            bool? hasUnresolvedDependency = null,
            ITargetFramework activeTarget = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IDependenciesSnapshot>(behavior);

            if (targets != null)
            {
                mock.Setup(x => x.Targets).Returns(targets.ToImmutableDictionary());
            }

            if (hasUnresolvedDependency != null && hasUnresolvedDependency.HasValue)
            {
                mock.Setup(x => x.HasUnresolvedDependency).Returns(hasUnresolvedDependency.Value);
            }

            if (activeTarget != null)
            {
                mock.Setup(x => x.ActiveTarget).Returns(activeTarget);
            }

            return mock.Object;
        }
    }
}
