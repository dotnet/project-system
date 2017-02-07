// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal interface IDependency : IEquatable<IDependency>, IComparable<IDependency>, IDependencyModel
    {
        /// <summary>
        /// Specifies if there is unresolved child somwhere in the dependency graph
        /// </summary>
        bool HasUnresolvedDependency { get; }

        /// <summary>
        /// A list of direct child dependencies
        /// </summary>
        IEnumerable<IDependency> Dependencies { get; }

        /// <summary>
        /// Targeted snapshot dependnecy belong to.
        /// </summary>
        ITargetedDependenciesSnapshot Snapshot { get; }

        string Alias { get; }

        IDependency SetProperties(
            string caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            IImmutableList<string> dependencyIDs = null);
    }
}
