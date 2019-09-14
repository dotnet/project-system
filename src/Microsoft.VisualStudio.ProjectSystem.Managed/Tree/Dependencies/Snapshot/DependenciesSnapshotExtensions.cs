// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class DependenciesSnapshotExtensions
    {
        public static IEnumerable<IDependency> GetFlatTopLevelDependencies(this DependenciesSnapshot self)
        {
            foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedSnapshot) in self.DependenciesByTargetFramework)
            {
                foreach (IDependency dependency in targetedSnapshot.TopLevelDependencies)
                {
                    yield return dependency;
                }
            }
        }
    }
}
