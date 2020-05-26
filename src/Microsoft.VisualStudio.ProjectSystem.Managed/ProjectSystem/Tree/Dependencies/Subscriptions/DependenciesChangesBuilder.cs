// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    internal sealed class DependenciesChangesBuilder
    {
        private HashSet<IDependencyModel>? _added;
        private HashSet<IDependencyModel>? _removed;

        public void Added(IDependencyModel model)
        {
            _added ??= new HashSet<IDependencyModel>(IDependencyModelEqualityComparer.Instance);

            _added.Remove(model);
            _added.Add(model);
        }

        public void Removed(string providerType, string dependencyId)
        {
            _removed ??= new HashSet<IDependencyModel>(IDependencyModelEqualityComparer.Instance);

            var identity = new RemovedDependencyModel(providerType, dependencyId);
            _removed.Remove(identity);
            _removed.Add(identity);
        }

        public IDependenciesChanges? TryBuildChanges()
        {
            if (_added == null && _removed == null)
            {
                return null;
            }

            return new DependenciesChanges(
                _added == null ? (IImmutableList<IDependencyModel>)ImmutableList<IDependencyModel>.Empty : ImmutableArray.CreateRange(_added),
                _removed == null ? (IImmutableList<IDependencyModel>)ImmutableList<IDependencyModel>.Empty : ImmutableArray.CreateRange(_removed));
        }

        public override string ToString() => ToString(_added, _removed);

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

            public override string ToString() => DependenciesChangesBuilder.ToString(AddedNodes, RemovedNodes);
        }

        private static string ToString(IReadOnlyCollection<IDependencyModel>? added, IReadOnlyCollection<IDependencyModel>? removed)
        {
            int addedCount = added?.Count ?? 0;
            int removedCount = removed?.Count ?? 0;

            return (addedCount, removedCount) switch
            {
                (0, 0) => "No changes",
                (0, _) => $"{removedCount} removed",
                (_, 0) => $"{addedCount} added",
                (_, _) => $"{addedCount} added, {removedCount} removed"
            };
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
            public string? SchemaName => throw NotImplemented();
            public string? SchemaItemType => throw NotImplemented();
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

            public override string ToString() => $"{ProviderType}-{Id}";
        }
    }
}
