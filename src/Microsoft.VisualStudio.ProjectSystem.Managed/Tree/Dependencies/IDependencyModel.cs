// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// A public model used to update dependencies in the snapshot.
    /// </summary>
    public interface IDependencyModel
    {
        /// <summary>
        /// Uniquely identifies the dependency within its project (for a given configuration).
        /// </summary>
        /// <remarks>
        /// For dependencies obtained via MSBuild, this equals <see cref="OriginalItemSpec"/>.
        /// </remarks>
        string Id { get; }

        /// <summary>
        /// Dependency type, a formal name of the provider type that knows how to create a node
        /// for given dependency.
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Name of the dependency
        /// </summary>
        string Name { get; }

        /// <summary>
        /// ItemSpec by which dependency could be found in msbuild Project. 
        ///     - If dependency is "Resolved" then resolved path will be in Path property, 
        ///       and unresolved in OriginalItemSpec.
        ///     - if dependency is "Unresolved" then Path and OriginalItemSpec are the same.
        ///     - if dependency is "custom", i.e. does not have item in the msbuild project or
        ///       item is not represented by xaml rule, then OriginalItemSpec will be ignored
        ///       and should be empty.
        /// </summary>
        string OriginalItemSpec { get; }

        /// <summary>
        /// When <see cref="Resolved"/> is <see langword="true"/>, this contains the resolved path
        /// of the dependency, otherwise it is equal to <see cref="OriginalItemSpec"/>.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Friendly name of the dependency, should be used for UI (captions etc)
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to determine the browse
        /// object rule for this dependency.
        /// </summary>
        string? SchemaName { get; }

        /// <summary>
        /// Used in <see cref="IDependenciesTreeServices.GetBrowseObjectRuleAsync"/> to determine the browse
        /// object rule for this dependency.
        /// </summary>
        string? SchemaItemType { get; }

        /// <summary>
        /// Version of the dependency
        /// </summary>
        [Obsolete("Property is unused.")]
        string Version { get; }

        /// <summary>
        /// Specifies if dependency is resolved or not
        /// </summary>
        bool Resolved { get; }

        /// <summary>
        /// Specifies if dependency is an explicit project dependency or not
        /// </summary>
        bool TopLevel { get; }

        /// <summary>
        /// Specifies if dependency was brought by default and can not be removed/modified by user.
        /// </summary>
        bool Implicit { get; }

        /// <summary>
        /// In some cases dependency should be present in snapshot, but not displayed in the Tree.
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is resolved and collapsed.
        /// </summary>
        ImageMoniker Icon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is resolved and expanded.
        /// </summary>
        ImageMoniker ExpandedIcon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is unresolved and collapsed.
        /// </summary>
        ImageMoniker UnresolvedIcon { get; }

        /// <summary>
        /// Gets the icon to use when the dependency is unresolved and expanded.
        /// </summary>
        ImageMoniker UnresolvedExpandedIcon { get; }

        /// <summary>
        /// Gets a value that determines this node's order relative to its peers.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     This behaviour only applies to graph nodes (i.e. children of top-level dependencies).
        /// </para>
        /// <para>
        ///     The default is zero, which means ordering will be alphabetical.
        ///     If non-zero, the node will be positioned after all nodes having lower priority.
        /// </para>
        /// </remarks>
        int Priority { get; }

        ProjectTreeFlags Flags { get; }

        /// <summary>
        /// A list of properties that might be displayed in property pages
        /// (in BrowsableObject context).
        /// </summary>
        IImmutableDictionary<string, string> Properties { get; }

        /// <summary>
        /// Gets the set of child dependency IDs. May be empty, but never <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Each ID is of the form provided by the dependency model.
        /// For dependencies obtained via MSBuild, these will be <see cref="OriginalItemSpec"/> values.
        /// </remarks>
        IImmutableList<string> DependencyIDs { get; }
    }
}
