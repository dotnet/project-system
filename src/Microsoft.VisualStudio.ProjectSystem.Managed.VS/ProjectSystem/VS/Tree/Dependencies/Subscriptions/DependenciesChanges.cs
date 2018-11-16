// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal sealed class DependenciesChanges : IDependenciesChanges
    {
        private readonly HashSet<IDependencyModel> _added = new HashSet<IDependencyModel>();
        private readonly HashSet<IDependencyModel> _removed = new HashSet<IDependencyModel>();

        public bool AnyChanges => _added.Count != 0 || _removed.Count != 0;

        public IImmutableList<IDependencyModel> AddedNodes
        {
            get
            {
                lock (_added)
                {
                    return ImmutableArray.CreateRange(_added);
                }
            }
        }

        public IImmutableList<IDependencyModel> RemovedNodes
        {
            get
            {
                lock (_removed)
                {
                    return ImmutableArray.CreateRange(_removed);
                }
            }
        }

        public void IncludeAddedChange(IDependencyModel model)
        {
            lock (_added)
            {
                _added.Remove(model);
                _added.Add(model);
            }
        }

        public void IncludeRemovedChange(string providerType, string dependencyId)
        {
            var identity = new RemovedDependencyModel(providerType, dependencyId);

            lock (_removed)
            {
                _removed.Remove(identity);
                _removed.Add(identity);
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

    internal static class IDependenciesChangesExtensions
    {
        public static bool AnyChanges(this IDependenciesChanges changes)
        {
            if (changes is DependenciesChanges c)
            {
                // Non-allocating path when using our own type.
                // Ideally this property would be on the IDependenciesChanges interface directly.
                return c.AnyChanges;
            }

            return changes.AddedNodes.Any() || changes.RemovedNodes.Any();
        }
    }
}
