// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Changes resolved top level project dependencies to unresolved if:
    ///     - dependent project does not have targets supporting given target framework in current project
    ///     - dependent project has any unresolved dependencies in a snapshot for given target framework
    /// This helps to bubble up error status (yellow icon) for project dependencies.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class ImplicitTopLevelDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 130;

        private IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }
        private ITargetFrameworkProvider TargetFrameworkProvider { get; }

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
            IDependency resultDependency = dependency;

            if (!resultDependency.TopLevel
                || resultDependency.Implicit                
                || !resultDependency.Resolved
                || !resultDependency.Flags.Contains(DependencyTreeFlags.GenericDependencyFlags))
            {
                return resultDependency;
            }

            if (projectItemSpecs == null)   // No data, so don't update
                return resultDependency;

            if (!projectItemSpecs.Contains(resultDependency.OriginalItemSpec))
            {
                // it is an implicit dependency
                if (!subTreeProviders.TryGetValue(resultDependency.ProviderType, out IProjectDependenciesSubTreeProvider provider))
                {
                    return resultDependency;
                }

                var internalProvider = provider as IProjectDependenciesSubTreeProviderInternal;
                if (internalProvider == null)
                {
                    return resultDependency;
                }

                resultDependency = resultDependency.SetProperties(
                    icon: internalProvider.GetImplicitIcon(),
                    expandedIcon: internalProvider.GetImplicitIcon(),
                    isImplicit: true);
                filterAnyChanges = true;
            }

            return resultDependency;
        }
    }
}
