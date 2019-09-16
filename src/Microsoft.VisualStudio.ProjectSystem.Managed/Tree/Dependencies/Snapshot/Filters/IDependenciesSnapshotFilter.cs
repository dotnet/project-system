// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Implementations of this interface control how the addition and removal of <see cref="IDependency"/>
    /// objects influences the sequence of <see cref="TargetedDependenciesSnapshot"/> objects.
    /// </summary>
    /// <remarks>
    /// For each added and removed dependency in an update, <see cref="TargetedDependenciesSnapshot.FromChanges"/>
    /// iterates through an ordered set of these filters, allowing each to influence how the dependency data is
    /// integrated into the next snapshot.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IDependenciesSnapshotFilter
    {
        /// <summary>
        /// Called before adding or updated a dependency to a snapshot.
        /// </summary>
        /// <remarks>
        /// When an <see cref="IDependency"/> is added or updated, implementations of this method may:
        /// <list type="bullet">
        ///   <item>accept the dependency as is,</item>
        ///   <item>accept the dependency but modify it before it is added/updated,</item>
        ///   <item>reject the dependency outright (though any previous state of the dependency is kept)</item>
        /// </list>
        /// In addition to the above operations, implementations of this method may also modify other
        /// dependencies in the snapshot. All these operations are performed via the <paramref name="context"/>.
        /// </remarks>
        /// <param name="targetFramework">Target framework for which dependency was resolved.</param>
        /// <param name="dependency">The dependency to which filter should be applied.</param>
        /// <param name="subTreeProviderByProviderType">All dictionary of subtree providers keyed by <see cref="IProjectDependenciesSubTreeProvider.ProviderType"/>.</param>
        /// <param name="projectItemSpecs">List of all items contained in project's xml at given moment (non-imported items), otherwise, <see langword="null"/> if we do not have any data.</param>
        /// <param name="context">An object via which the filter must signal acceptance or rejection, in addition to making further changes to other dependencies.</param>
        void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context);

        /// <summary>
        /// Called before removing a dependency from a snapshot.
        /// </summary>
        /// <param name="targetFramework">Target framework for which dependency was resolved.</param>
        /// <param name="dependency">The dependency to which filter should be applied.</param>
        /// <param name="context">An object via which the filter must signal acceptance or rejection, in addition to making further changes to other dependencies.</param>
        void BeforeRemove(
            ITargetFramework targetFramework,
            IDependency dependency,
            RemoveDependencyContext context);
    }
}
