// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Represents internal immutable dependency entity that is stored in immutable 
    /// snapshot <see cref="ITargetedDependenciesSnapshot"/>.
    /// </summary>
    internal interface IDependency : IEquatable<IDependency>, IComparable<IDependency>, IDependencyModel
    {
        /// <summary>
        /// Target framework of the snapshot dependency belongs to
        /// </summary>
        ITargetFramework TargetFramework { get; }

        /// <summary>
        /// Alias is used to de-dupe tree nodes in the CPS tree. If there are seberal nodes in the same
        /// folder with the same name, we replace them all with: Alias = "Caption (OriginalItemSpec)".
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// IDependency is immutable and sometimes tree view provider or snapshot filters need 
        /// to change some properties of a given dependency. This method creates a new instance 
        /// of IDependency with new properties set.
        /// </summary>
        IDependency SetProperties(
            string caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string schemaName = null,
            IImmutableList<string> dependencyIDs = null,
            ImageMoniker icon = new ImageMoniker(),
            bool? isImplicit = null);
    }
}
