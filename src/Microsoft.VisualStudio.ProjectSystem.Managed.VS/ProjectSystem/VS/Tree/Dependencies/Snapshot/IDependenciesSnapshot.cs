// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Immutable snapshot of all project dependencies across all target frameworks.
    /// </summary>
    internal interface IDependenciesSnapshot : IEquatable<IDependenciesSnapshot>
    {
        /// <summary>
        /// Gets the path to the project whose dependencies this snapshot contains.
        /// </summary>
        string ProjectPath { get; }

        /// <summary>
        /// Gets the active target framework for project (first in the <c>TargetFrameworks</c> property).
        /// </summary>
        ITargetFramework ActiveTarget { get; }

        /// <summary>
        /// Gets a dictionary of dependencies by target framework.
        /// </summary>
        ImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> Targets { get; }

        /// <summary>
        /// Gets whether this snapshot contains at least one unresolved/broken dependency at any level
        /// for any target framework.
        /// </summary>
        bool HasUnresolvedDependency { get; }

        /// <summary>
        /// Finds dependency for given id across all target frameworks.
        /// </summary>
        /// <param name="dependencyId">Unique id for dependency to be found.</param>
        /// <param name="topLevel">If <see langword="true"/>, search is first performed on top level
        /// dependencies before searching all dependencies.</param>
        /// <returns>The <see cref="IDependency"/> if found, otherwise <see langword="null"/>.</returns>
        IDependency FindDependency(string dependencyId, bool topLevel = false);
    }
}
