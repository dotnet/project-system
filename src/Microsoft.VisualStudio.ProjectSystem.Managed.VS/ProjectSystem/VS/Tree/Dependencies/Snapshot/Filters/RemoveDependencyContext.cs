// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    internal sealed class RemoveDependencyContext : IRemoveDependencyContext
    {
        private readonly ImmutableDictionary<string, IDependency>.Builder _worldBuilder;

        private bool? _acceptedOrRejected;
        private IDependency _acceptedDependency;

        public bool Changed { get; private set; }

        public RemoveDependencyContext(ImmutableDictionary<string, IDependency>.Builder worldBuilder)
        {
            _worldBuilder = worldBuilder;
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

        public void Accept()
        {
            if (_acceptedOrRejected != null)
            {
                throw new InvalidOperationException(
                    $"Filter has already {(_acceptedDependency == null ? "rejected the" : "accepted a")} dependency.");
            }

            _acceptedOrRejected = true;
        }

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
