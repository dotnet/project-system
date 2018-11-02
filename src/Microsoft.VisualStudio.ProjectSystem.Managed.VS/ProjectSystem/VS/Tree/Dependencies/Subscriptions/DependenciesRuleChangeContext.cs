// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal sealed class DependenciesRuleChangeContext : IRuleChangeContext
    {
        private readonly object _changesLock = new object();

        public DependenciesRuleChangeContext(ITargetFramework activeTarget, IProjectCatalogSnapshot catalogs)
        {
            ActiveTarget = activeTarget;
            Catalogs = catalogs;
        }

        public ITargetFramework ActiveTarget { get; }

        public IProjectCatalogSnapshot Catalogs { get; }

        public ImmutableDictionary<ITargetFramework, IDependenciesChanges> Changes { get; private set; } = ImmutableDictionary.Create<ITargetFramework, IDependenciesChanges>();

        public bool AnyChanges => Changes.Count != 0;

        public void IncludeAddedChange(ITargetFramework targetFramework, IDependencyModel ruleMetadata)
        {
            GetChanges(targetFramework).IncludeAddedChange(ruleMetadata);
        }

        public void IncludeRemovedChange(ITargetFramework targetFramework, IDependencyModel ruleMetadata)
        {
            GetChanges(targetFramework).IncludeRemovedChange(ruleMetadata);
        }

        private DependenciesChanges GetChanges(ITargetFramework targetFramework)
        {
            lock (_changesLock)
            {
                if (!Changes.TryGetValue(targetFramework, out IDependenciesChanges change))
                {
                    change = new DependenciesChanges();
                    Changes = Changes.Add(targetFramework, change);
                }

                // We only add DependenciesChanges to this collection, so the cast is safe
                return (DependenciesChanges)change;
            }
        }
    }
}
