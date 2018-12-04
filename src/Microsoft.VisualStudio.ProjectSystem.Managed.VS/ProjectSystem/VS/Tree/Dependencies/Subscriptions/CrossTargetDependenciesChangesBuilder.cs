// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal sealed class CrossTargetDependenciesChangesBuilder
    {
        private Dictionary<ITargetFramework, DependenciesChangesBuilder> _changes;

        public ImmutableDictionary<ITargetFramework, IDependenciesChanges> TryBuildChanges()
        {
            return _changes?.ToImmutableDictionary(
                pair => pair.Key,
                pair => pair.Value.Build());
        }

        public void Added(ITargetFramework targetFramework, IDependencyModel model)
        {
            GetChanges(targetFramework).Added(model);
        }

        public void Removed(ITargetFramework targetFramework, string providerType, string dependencyId)
        {
            GetChanges(targetFramework).Removed(providerType, dependencyId);
        }

        private DependenciesChangesBuilder GetChanges(ITargetFramework targetFramework)
        {
            if (_changes == null)
            {
                _changes = new Dictionary<ITargetFramework, DependenciesChangesBuilder>();
            }

            if (_changes.TryGetValue(targetFramework, out DependenciesChangesBuilder builder))
            {
                return builder;
            }

            return _changes[targetFramework] = new DependenciesChangesBuilder();
        }
    }
}
