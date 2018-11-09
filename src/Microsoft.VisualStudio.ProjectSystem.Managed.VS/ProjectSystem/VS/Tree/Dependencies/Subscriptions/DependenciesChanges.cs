// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal sealed class DependenciesChanges : IDependenciesChanges
    {
        private readonly HashSet<IDependencyModel> _added = new HashSet<IDependencyModel>();
        private readonly HashSet<(string providerType, string dependencyId)> _removed = new HashSet<(string providerType, string dependencyId)>();

        public bool AnyChanges => _added.Count != 0 || _removed.Count != 0;

        public ImmutableArray<IDependencyModel> AddedNodes
        {
            get
            {
                lock (_added)
                {
                    return ImmutableArray.CreateRange(_added);
                }
            }
        }

        public ImmutableArray<(string providerType, string dependencyId)> RemovedNodes
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
                _added.Add(model);
            }
        }

        public void IncludeRemovedChange(string providerType, string dependencyId)
        {
            lock (_removed)
            {
                _removed.Add((providerType, dependencyId));
            }
        }
    }
}
