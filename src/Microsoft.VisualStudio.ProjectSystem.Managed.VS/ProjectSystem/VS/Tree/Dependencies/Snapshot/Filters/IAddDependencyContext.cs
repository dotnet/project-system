// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Context used by <see cref="IDependenciesSnapshotFilter"/> implementations when filtering
    /// a dependency that is being added to a <see cref="DependenciesSnapshot"/> by
    /// <see cref="DependenciesSnapshot.FromChanges"/>.
    /// </summary>
    internal interface IAddDependencyContext
    {
        /// <summary>
        /// Indicates the filter wishes to add <paramref name="dependency"/> to the snapshot.
        /// </summary>
        /// <remarks>
        /// Note that this may not be the instance the filter was provided, as it may mutate it
        /// in some way prior to adding it to the snapshot. Also note that when multiple filters
        /// exist, later filters may further mutate or reject this filter's accepted dependency.
        /// </remarks>
        /// <param name="dependency">The dependency to add to the snapshot.</param>
        void Accept(IDependency dependency);

        /// <summary>
        /// Indicates the filter rejects the addition or updating of the dependency in the snapshot.
        /// </summary>
        void Reject();

        /// <summary>
        /// Adds a new, or replaces an existing dependency (keyed on <see cref="IDependencyModel.Id"/>).
        /// </summary>
        /// <remarks>
        /// In the course of filtering one dependency, the filter may wish to modify or add other
        /// dependencies in the project's tree. This method allows that to happen.
        /// </remarks>
        void AddOrUpdate(IDependency dependency);

        /// <summary>
        /// Attempts to find the dependency in the project's tree with specified <paramref name="dependencyId"/>.
        /// </summary>
        bool TryGetDependency(string dependencyId, out IDependency dependency);

        /// <summary>
        /// Returns <see langword="true"/> if the project tree contains a dependency with specified <paramref name="dependencyId"/>.
        /// </summary>
        bool Contains(string dependencyId);

        /// <summary>
        /// Returns an enumerator over all dependencies in the project tree.
        /// </summary>
        /// <returns></returns>
        ImmutableDictionary<string, IDependency>.Enumerator GetEnumerator();
    }
}
