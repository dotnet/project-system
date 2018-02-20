// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Filter does not allow unresolved dependency rule to override resolved one in the snapshot. 
    /// When project changes and old resolved dependency cannot be resolved anymore, only removed
    /// resolved dependency rule can delete old dependency (not unresolved rule).
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class UnresolvedDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 100;

        public override IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency, 
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            Dictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs,
            out bool filterAnyChanges)
        {
            filterAnyChanges = false;

            if (dependency.Resolved == false && worldBuilder.Keys.Contains(dependency.Id))
            {
                return null;
            }

            return dependency;
        }
    }
}
