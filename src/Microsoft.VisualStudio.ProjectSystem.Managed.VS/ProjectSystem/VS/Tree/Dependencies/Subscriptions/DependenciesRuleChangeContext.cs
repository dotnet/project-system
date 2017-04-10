// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal class DependenciesRuleChangeContext : IRuleChangeContext
    {
        private readonly object _changesLock = new object();

        public DependenciesRuleChangeContext(ITargetFramework target, IProjectCatalogSnapshot catalogs)
        {
            ActiveTarget = target;
            Catalogs = catalogs;
        }

        public ITargetFramework ActiveTarget { get; }
        public IProjectCatalogSnapshot Catalogs { get; }
        private ImmutableDictionary<ITargetFramework, IDependenciesChanges> _changes =
            ImmutableDictionary.Create<ITargetFramework, IDependenciesChanges>();
        public ImmutableDictionary<ITargetFramework, IDependenciesChanges> Changes
        {
            get
            {
                lock (_changesLock)
                {
                    return _changes;
                }
            }
        }

        public bool AnyChanges
        {
            get
            {
                return Changes.Count > 0;
            }
        }

        public void IncludeAddedChange(ITargetFramework targetFramework,
                                       IDependencyModel ruleMetadata)
        {
            lock (_changesLock)
            {
                var change = GetChanges(targetFramework) as DependenciesChanges;
                change?.ExcludeAddedChange(ruleMetadata);
                change?.IncludeAddedChange(ruleMetadata);
            }
        }

        public void IncludeRemovedChange(ITargetFramework targetFramework,
                                         IDependencyModel ruleMetadata)
        {
            lock (_changesLock)
            {
                var change = GetChanges(targetFramework) as DependenciesChanges;
                change?.ExcludeRemovedChange(ruleMetadata);
                change?.IncludeRemovedChange(ruleMetadata);
            }
        }

        private IDependenciesChanges GetChanges(ITargetFramework targetFramework)
        {
            if (!_changes.TryGetValue(targetFramework, out IDependenciesChanges change))
            {
                change = new DependenciesChanges();
                _changes = _changes.Add(targetFramework, change);
            }

            return change;
        }
    }
}
