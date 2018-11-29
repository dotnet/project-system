// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal sealed class DependenciesChangesBuilder
    {
        private HashSet<IDependencyModel> _added;
        private HashSet<IDependencyModel> _removed;

        public void Added(IDependencyModel model)
        {
            if (_added == null)
            {
                _added = new HashSet<IDependencyModel>();
            }

            _added.Remove(model);
            _added.Add(model);
        }

        public void Removed(string providerType, string dependencyId)
        {
            if (_removed == null)
            {
                _removed = new HashSet<IDependencyModel>();
            }

            var identity = new RemovedDependencyModel(providerType, dependencyId);
            _removed.Remove(identity);
            _removed.Add(identity);
        }

        public IDependenciesChanges Build()
        {
            return new DependenciesChanges(
                _added   == null ? (IImmutableList<IDependencyModel>)ImmutableList<IDependencyModel>.Empty : ImmutableArray.CreateRange(_added),
                _removed == null ? (IImmutableList<IDependencyModel>)ImmutableList<IDependencyModel>.Empty : ImmutableArray.CreateRange(_removed));
        }

        private sealed class DependenciesChanges : IDependenciesChanges
        {
            public IImmutableList<IDependencyModel> AddedNodes { get; }
            public IImmutableList<IDependencyModel> RemovedNodes { get; }

            public DependenciesChanges(
                IImmutableList<IDependencyModel> addedNodes,
                IImmutableList<IDependencyModel> removedNodes)
            {
                AddedNodes = addedNodes;
                RemovedNodes = removedNodes;
            }
        }

        private sealed class RemovedDependencyModel : IDependencyModel
        {
            public RemovedDependencyModel(string providerType, string dependencyId)
            {
                ProviderType = providerType;
                Id = dependencyId;
            }

            public string Id { get; }
            public string ProviderType { get; }

            // No other property should be used for removal.
            //
            // We intended to change the type of IDependenciesChanges.RemovedNodes
            // but due to cross-team scheduling went this route to maintain compatibility.

            private static Exception NotImplemented() => throw new NotImplementedException("When removing dependencies, a full model is not available.");

            public string Name => throw NotImplemented();
            public string OriginalItemSpec => throw NotImplemented();
            public string Path => throw NotImplemented();
            public string Caption => throw NotImplemented();
            public string SchemaName => throw NotImplemented();
            public string SchemaItemType => throw NotImplemented();
            public string Version => throw NotImplemented();
            public bool Resolved => throw NotImplemented();
            public bool TopLevel => throw NotImplemented();
            public bool Implicit => throw NotImplemented();
            public bool Visible => throw NotImplemented();
            public ImageMoniker Icon => throw NotImplemented();
            public ImageMoniker ExpandedIcon => throw NotImplemented();
            public ImageMoniker UnresolvedIcon => throw NotImplemented();
            public ImageMoniker UnresolvedExpandedIcon => throw NotImplemented();
            public int Priority => throw NotImplemented();
            public ProjectTreeFlags Flags => throw NotImplemented();
            public IImmutableDictionary<string, string> Properties => throw NotImplemented();
            public IImmutableList<string> DependencyIDs => throw NotImplemented();

            public override bool Equals(object obj)
                => obj is RemovedDependencyModel other &&
                   string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(ProviderType, other.ProviderType, StringComparisons.DependencyProviderTypes);

            public override int GetHashCode()
                => unchecked(StringComparer.OrdinalIgnoreCase.GetHashCode(Id) * 397 ^
                             StringComparer.OrdinalIgnoreCase.GetHashCode(ProviderType));
        }
    }
}
