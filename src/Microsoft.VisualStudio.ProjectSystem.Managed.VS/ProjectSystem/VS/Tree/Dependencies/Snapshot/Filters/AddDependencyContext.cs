// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    internal sealed class AddDependencyContext : IAddDependencyContext
    {
        private readonly ImmutableDictionary<string, IDependency>.Builder _worldBuilder;

        private bool _acceptedOrRejected;
        private IDependency _acceptedDependency;

        public bool Changed { get; private set; }

        public AddDependencyContext(ImmutableDictionary<string, IDependency>.Builder worldBuilder)
        {
            _worldBuilder = worldBuilder;
        }

        public void Reset()
        {
            Changed = false;
            _acceptedOrRejected = false;
            _acceptedDependency = null;
        }

        public IDependency GetResult(IDependenciesSnapshotFilter filter)
        {
            if (!_acceptedOrRejected)
            {
                throw new InvalidOperationException(
                    $"Filter type {filter.GetType()} must either accept or reject the dependency.");
            }

            _acceptedOrRejected = false;
            return _acceptedDependency;
        }

        public bool TryGetDependency(string dependencyId, out IDependency dependency)
        {
            return _worldBuilder.TryGetValue(dependencyId, out dependency);
        }

        public void AddOrUpdate(IDependency dependency)
        {
            _worldBuilder.Remove(dependency.Id);
            _worldBuilder.Add(dependency.Id, dependency);
            Changed = true;
        }

        public bool Contains(string dependencyId)
        {
            return _worldBuilder.ContainsKey(dependencyId);
        }

        public ImmutableDictionary<string, IDependency>.Enumerator GetEnumerator()
        {
            return _worldBuilder.GetEnumerator();
        }

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
