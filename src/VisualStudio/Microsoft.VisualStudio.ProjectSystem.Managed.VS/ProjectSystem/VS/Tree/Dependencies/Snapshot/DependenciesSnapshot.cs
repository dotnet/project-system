// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal class DependenciesSnapshot: IDependenciesSnapshot 
    {
        private DependenciesSnapshot(string projectPath,
                                     ITargetFramework activeTarget = null,
                                     DependenciesSnapshot previousSnapshot = null)
        {
            Requires.NotNullOrEmpty(projectPath, nameof(projectPath));

            ProjectPath = projectPath;
            ActiveTarget = activeTarget;

            if (previousSnapshot != null)
            {
                _targets = previousSnapshot._targets;
                if (ActiveTarget == null)
                {
                    ActiveTarget = previousSnapshot.ActiveTarget;
                }
            }

            if (ActiveTarget == null)
            {
                ActiveTarget = TargetFramework.Empty;
            }
        }

        public string ProjectPath { get; }

        public ITargetFramework ActiveTarget { get; }

        private ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> _targets =
            ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot>.Empty;

        public IImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> Targets
        {
            get
            {
                return _targets;
            }
            private set
            {
                _targets = value.ToImmutableDictionary();
            }
        }

        public bool HasUnresolvedDependency
        {
            get
            {
                return Targets.Any(x => x.Value.HasUnresolvedDependency);
            }
        }

        public IDependency FindDependency(string id, bool topLevel = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (topLevel)
            {
                // if top level first try to find by top level id with full path,
                // if found - return, if not - try regular Id in the DependenciesWorld
                foreach (var target in Targets)
                {
                    var dependency = target.Value.TopLevelDependencies.FirstOrDefault(
                        x => x.GetTopLevelId().Equals(id, StringComparison.OrdinalIgnoreCase));
                    if (dependency != null)
                    {
                        return dependency;
                    }
                }
            }

            foreach (var target in Targets)
            {
                if (target.Value.DependenciesWorld.TryGetValue(id, out IDependency dependency))
                {
                    return dependency;
                }
            }

            return null;
        }

        private bool MergeChanges(
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters,
            IEnumerable<IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs)
        {
            var anyChanges = false;
            var builder = _targets.ToBuilder();
            foreach(var change in changes)
            {
                builder.TryGetValue(change.Key, out ITargetedDependenciesSnapshot previousSnapshot);
                var newTargetedSnapshot = TargetedDependenciesSnapshot.FromChanges(
                                            ProjectPath,
                                            change.Key, 
                                            previousSnapshot, 
                                            change.Value,
                                            catalogs,
                                            snapshotFilters,
                                            subTreeProviders,
                                            projectItemSpecs,
                                            out bool anyTfmChanges);
                builder[change.Key] = newTargetedSnapshot;

                if (anyTfmChanges)
                {
                    anyChanges = true;
                }
            }

            // now get rid of empty target frameworks (if there no any dependencies for them)
            foreach(var targetKvp in builder.ToList())
            {
                if (targetKvp.Value.DependenciesWorld.Count <= 0)
                {
                    anyChanges = true;
                    builder.Remove(targetKvp.Key);
                }
            }

            _targets = builder.ToImmutableDictionary();

            return anyChanges;
        }

        public bool Equals(IDependenciesSnapshot other)
        {
            if (other != null && other.ProjectPath.Equals(ProjectPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static DependenciesSnapshot CreateEmpty(string projectPath)
        {
            return new DependenciesSnapshot(projectPath);
        }

        public static DependenciesSnapshot FromChanges(
            string projectPath,
            DependenciesSnapshot previousSnapshot,
            ImmutableDictionary<ITargetFramework, IDependenciesChanges> changes,
            IProjectCatalogSnapshot catalogs,
            ITargetFramework activeTargetFramework,
            IEnumerable<IDependenciesSnapshotFilter> snapshotFilters,
            IEnumerable<IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs,
            out bool anyChanges)
        {
            var newSnapshot = new DependenciesSnapshot(projectPath, activeTargetFramework, previousSnapshot);
            anyChanges = newSnapshot.MergeChanges(changes, catalogs, snapshotFilters, subTreeProviders, projectItemSpecs);
            return newSnapshot;
        }

        public DependenciesSnapshot RemoveTargets(IEnumerable<ITargetFramework> targetToRemove)
        {
            var newSnapshot = new DependenciesSnapshot(ProjectPath, ActiveTarget, this);
            newSnapshot.Targets = newSnapshot.Targets.RemoveRange(targetToRemove);
            return newSnapshot;
        }
    }
}
