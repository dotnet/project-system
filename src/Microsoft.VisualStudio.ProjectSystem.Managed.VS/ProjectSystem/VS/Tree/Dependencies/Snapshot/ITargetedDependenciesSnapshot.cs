// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Immutable snapshot of all project dependencies for given <see cref="ITargetFramework"/>.
    /// </summary>
    internal interface ITargetedDependenciesSnapshot
    {
        /// <summary>
        /// Path to project containing this snapshot.
        /// </summary>
        string ProjectPath { get; }

        /// <summary>
        /// <see cref="ITargetFramework" /> for which project has dependencies contained in this snapshot.
        /// </summary>
        ITargetFramework TargetFramework { get; }

        /// <summary>
        /// Catalogs of rules for project items (optional, custom dependency providers might not provide it).
        /// </summary>
        IProjectCatalogSnapshot Catalogs { get; }

        /// <summary>
        /// Top level project dependencies.
        /// </summary>
        ImmutableHashSet<IDependency> TopLevelDependencies { get; }

        /// <summary>
        /// Flat hash table of all unique dependencies in the project (from all levels). Having this table,
        /// a given dependency A can find it's actual <see cref="IDependency"/> children having their string
        /// ids.
        /// </summary>
        ImmutableDictionary<string, IDependency> DependenciesWorld { get; }

        /// <summary>
        /// Specifies is this snapshot contains at least one unresolved/broken dependency at any level.
        /// </summary>
        bool HasUnresolvedDependency { get; }

        /// <summary>
        /// Efficient API for checking if a given dependency has an unresolved child dependency at any level. 
        /// </summary>
        /// <param name="dependency"></param>
        /// <returns>Returns true if given dependency has unresolved child dependency at any level</returns>
        bool CheckForUnresolvedDependencies(IDependency dependency);

        /// <summary>
        /// Efficient API for checking if a there is at least one unresolved dependency with given provider type.
        /// </summary>
        /// <param name="providerType">Provider type to check</param>
        /// <returns>Returns true if there is at least one unresolved dependency with given providerType.</returns>
        bool CheckForUnresolvedDependencies(string providerType);
    }
}
