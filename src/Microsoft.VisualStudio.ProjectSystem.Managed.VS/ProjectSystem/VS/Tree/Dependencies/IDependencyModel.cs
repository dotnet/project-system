// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Includes information about dependency and it's target framework for identification
        /// </summary>
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
        /// Path to the dependency when known
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Friendly name of the dependency, should be used for UI (captions etc)
        /// </summary>
        string Caption { get; }

        string SchemaName { get; }

        string SchemaItemType { get; }

        /// <summary>
        /// Version of the dependency
        /// </summary>
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
        /// Node's regular icon
        /// </summary>
        ImageMoniker Icon { get; }

        /// <summary>
        /// Node's expanded icon, if not provided regular icon should be used
        /// </summary>
        ImageMoniker ExpandedIcon { get; }

        /// <summary>
        /// Unresolved node's regular icon
        /// </summary>
        ImageMoniker UnresolvedIcon { get; }

        /// <summary>
        /// Unresolved node's expanded icon, if not provided regular icon should be used
        /// </summary>
        ImageMoniker UnresolvedExpandedIcon { get; }

        /// <summary>
        /// Priority specifies node's order among it's peers. Default is 0 and it means node will 
        /// be positioned according it's name in alphabethical order. If it is not 0, then node is 
        /// positioned after all nodes having lower priority. 
        /// Note: This is property is in effect only for graph nodes.
        /// </summary>
        int Priority { get; }

        ProjectTreeFlags Flags { get; }

        /// <summary>
        /// A list of properties that might be displayed in property pages
        /// (in BrowsableObject context).
        /// </summary>
        IImmutableDictionary<string, string> Properties { get; }

        IImmutableList<string> DependencyIDs { get; }
    }
}
