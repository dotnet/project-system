// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Context used by <see cref="IDependenciesSnapshotFilter"/> implementations when filtering
    /// a dependency that is being removed from a <see cref="DependenciesSnapshot"/> by
    /// <see cref="DependenciesSnapshot.FromChanges"/>.
    /// </summary>
    internal sealed class RemoveDependencyContext
    {
        private readonly Dictionary<string, IDependency> _dependencyById;

        private bool? _acceptedOrRejected;
        private IDependency? _acceptedDependency;

        public bool Changed { get; private set; }

        public RemoveDependencyContext(Dictionary<string, IDependency> dependencyById)
        {
            _dependencyById = dependencyById;
        }

        public void Reset()
        {
            Changed = false;
            _acceptedOrRejected = null;
            _acceptedDependency = null;
        }

        public bool GetResult(IDependenciesSnapshotFilter filter)
        {
            if (_acceptedOrRejected == null)
            {
                throw new InvalidOperationException(
                    $"Filter type {filter.GetType()} must either accept or reject the dependency.");
            }

            bool? value = _acceptedOrRejected;
            _acceptedOrRejected = null;
            return value.Value;
        }

        /// <summary>
        /// Attempts to find the dependency in the project's tree with specified <paramref name="dependencyId"/>.
        /// </summary>
        public bool TryGetDependency(string dependencyId, out IDependency dependency)
        {
            return _dependencyById.TryGetValue(dependencyId, out dependency);
        }

        /// <summary>
        /// Adds a new, or replaces an existing dependency (keyed on <see cref="IDependency.Id"/>).
        /// </summary>
        /// <remarks>
        /// In the course of filtering one dependency, the filter may wish to modify or add other
        /// dependencies in the project's tree. This method allows that to happen.
        /// </remarks>
        public void AddOrUpdate(IDependency dependency)
        {
            _dependencyById.Remove(dependency.Id);
            _dependencyById.Add(dependency.Id, dependency);
            Changed = true;
        }

        /// <summary>
        /// Indicates the filter allows the removal of the dependency from the snapshot.
        /// </summary>
        public void Accept()
        {
            if (_acceptedOrRejected != null)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = true;
        }

        /// <summary>
        /// Indicates the filter rejects the removal of the dependency from the snapshot.
        /// </summary>
        public void Reject()
        {
            if (_acceptedOrRejected != null)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = false;
        }
    }
}
