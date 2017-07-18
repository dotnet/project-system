// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Immutable snapshot containing all dependencies for all project's target frameworks.
    /// </summary>
    internal interface IDependenciesSnapshot : IEquatable<IDependenciesSnapshot>
    {
        /// <summary>
        /// Path to current project
        /// </summary>
        string ProjectPath { get; }

        /// <summary>
        /// Active target framework for project (first in the TargetFrameworks property)
        /// </summary>
        ITargetFramework ActiveTarget { get; }

        /// <summary>
        /// Hash table of snapshots of dependencies for individual target frameworks
        /// </summary>
        IImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> Targets { get; }

        /// <summary>
        /// Specifies is this snapshot contains at least one unresolved/broken dependency at any level
        /// for any target framework.
        /// </summary>
        bool HasUnresolvedDependency { get; }

        /// <summary>
        /// Finds dependency for given id accross all target frameworks.
        /// </summary>
        /// <param name="id">Unique id for dependency to be found.</param>
        /// <param name="topLevel">Suggests that id is specified for top level dependency and 
        /// different logic should be used, while searching for it.
        /// </param>
        /// <returns>IDependency object if given id was found, otherwise null.</returns>
        IDependency FindDependency(string id, bool topLevel = false);
    }
}
