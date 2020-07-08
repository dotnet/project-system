// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Context used by <see cref="IDependenciesSnapshotFilter"/> implementations when filtering
    /// a dependency that is being added to a <see cref="DependenciesSnapshot"/> by
    /// <see cref="DependenciesSnapshot.FromChanges"/>.
    /// </summary>
    internal sealed class AddDependencyContext
    {
        private readonly Dictionary<DependencyId, IDependency> _dependencyById;

        private bool _acceptedOrRejected;
        private IDependency? _acceptedDependency;

        public bool Changed { get; private set; }

        public AddDependencyContext(Dictionary<DependencyId, IDependency> dependencyById)
        {
            _dependencyById = dependencyById;
        }

        public void Reset()
        {
            Changed = false;
            _acceptedOrRejected = false;
            _acceptedDependency = null;
        }

        public IDependency? GetResult(IDependenciesSnapshotFilter filter)
        {
            if (!_acceptedOrRejected)
            {
                throw new InvalidOperationException(
                    $"Filter type {filter.GetType()} must either accept or reject the dependency.");
            }

            _acceptedOrRejected = false;
            return _acceptedDependency;
        }

        /// <summary>
        /// Attempts to find the dependency in the project's tree with specified <paramref name="dependencyId"/>.
        /// </summary>
        public bool TryGetDependency(DependencyId dependencyId, out IDependency dependency)
        {
            return _dependencyById.TryGetValue(dependencyId, out dependency);
        }

        /// <summary>
        /// Adds a new, or replaces an existing dependency (keyed on <see cref="IDependency.ProviderType"/> and <see cref="IDependency.Id"/>).
        /// </summary>
        /// <remarks>
        /// In the course of filtering one dependency, the filter may wish to modify or add other
        /// dependencies in the project's tree. This method allows that to happen.
        /// </remarks>
        public void AddOrUpdate(IDependency dependency)
        {
            DependencyId key = dependency.GetDependencyId();
            _dependencyById.Remove(key);
            _dependencyById.Add(key, dependency);
            Changed = true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the project tree contains a dependency with specified <paramref name="dependencyId"/>.
        /// </summary>
        public bool Contains(DependencyId dependencyId)
        {
            return _dependencyById.ContainsKey(dependencyId);
        }

        /// <summary>
        /// Returns an enumerator over all dependencies in the project tree.
        /// </summary>
        public Dictionary<DependencyId, IDependency>.Enumerator GetEnumerator()
        {
            return _dependencyById.GetEnumerator();
        }

        /// <summary>
        /// Indicates the filter wishes to add <paramref name="dependency"/> to the snapshot.
        /// </summary>
        /// <remarks>
        /// Note that this may not be the instance the filter was provided, as it may mutate it
        /// in some way prior to adding it to the snapshot. Also note that when multiple filters
        /// exist, later filters may further mutate or reject this filter's accepted dependency.
        /// </remarks>
        /// <param name="dependency">The dependency to add to the snapshot.</param>
        public void Accept(IDependency dependency)
        {
            Requires.NotNull(dependency, nameof(dependency));

            if (_acceptedOrRejected)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = true;
            _acceptedDependency = dependency;
        }

        /// <summary>
        /// Indicates the filter rejects the addition or updating of the dependency in the snapshot.
        /// </summary>
        public void Reject()
        {
            if (_acceptedOrRejected)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = true;
            _acceptedDependency = null;
        }
    }
}
