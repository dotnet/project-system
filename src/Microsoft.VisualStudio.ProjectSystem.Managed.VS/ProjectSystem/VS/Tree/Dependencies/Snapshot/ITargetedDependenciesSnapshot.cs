// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal interface ITargetedDependenciesSnapshot
    {
        string ProjectPath { get; }
        ITargetFramework TargetFramework { get; }
        IProjectCatalogSnapshot Catalogs { get; }
        ImmutableHashSet<IDependency> TopLevelDependencies { get; }
        ImmutableDictionary<string, IDependency> DependenciesWorld { get; }
        bool HasUnresolvedDependency { get; }
        bool CheckForUnresolvedDependencies(IDependency dependency);
        bool CheckForUnresolvedDependencies(string providerType);
    }
}
