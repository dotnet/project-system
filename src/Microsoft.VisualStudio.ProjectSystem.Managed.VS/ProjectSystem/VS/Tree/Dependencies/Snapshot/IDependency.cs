// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Represents internal immutable dependency entity that is stored in immutable 
    /// snapshot <see cref="ITargetedDependenciesSnapshot"/>.
    /// </summary>
    internal interface IDependency : IEquatable<IDependency>
    {
        /// <summary>
        /// Target framework of the snapshot dependency belongs to
        /// </summary>
        ITargetFramework TargetFramework { get; }

        /// <summary>
        /// Get the full path of the dependency, if relevant, otherwise, <see cref="string.Empty"/>.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// Alias is used to de-dupe tree nodes in the CPS tree. If there are several nodes in the same
        /// folder with the same name, we replace them all with: <c>Alias = "Caption (OriginalItemSpec)"</c>.
        /// </summary>
        string Alias { get; }

        DependencyIconSet IconSet { get; }

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
            DependencyIconSet iconSet = null,
            bool? isImplicit = null);

        #region Copied from IDependencyModel

        /// <summary>
        /// Includes information about dependency and its target framework for identification
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
        /// Priority specifies node's order among it's peers. Default is 0 and it means node will 
        /// be positioned according it's name in alphabetical order. If it is not 0, then node is 
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

        #endregion
    }
}
