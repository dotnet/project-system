// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// A data representation for project tree that is received from dependencies
    /// tree providers. It contains only data and is abstracted from actual nodes 
    /// that are displayed in the tree (IProjectTree or GraphNode).
    /// </summary>
    public interface IDependencyNode
    {
        /// <summary>
        /// An explicit unique ID of the node. In most cases, it can be equal to IProjectTree.FilePath
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Node's caption displayed in the tree
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Node's regular icon
        /// </summary>
        ImageMoniker Icon { get; }

        /// <summary>
        /// Node's expanded icon, if not provided regular icon should be used
        /// </summary>
        ImageMoniker ExpandedIcon { get; }

        /// <summary>
        /// Priority specifies node's order among it's peers. Default is 0 and it means node will 
        /// be positioned according it's name in alphabethical order. If it is not 0, then node is 
        /// positioned after all nodes having lower priority. 
        /// Note: This is property is in effect only for graph nodes.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Project tree flags specific to given node.
        /// </summary>
        ProjectTreeFlags Flags { get; }

        /// <summary>
        /// Children of current node.
        /// </summary>
        IEnumerable<IDependencyNode> Children { get; } // TODO Should we use ImmutableList here?

        /// <summary>
        /// Quick check if node has children.
        /// </summary>
        bool HasChildren { get; }

        /// <summary>
        /// Provider that owns the node
        /// </summary>
        IProjectDependenciesSubTreeProvider Provider { get; }

        /// <summary>
        /// A list of node's properties that might be displayed in property pages
        /// (in BrowsableObject context).
        /// </summary>
        IImmutableDictionary<string, string> Properties { get; }

        /// <summary>
        /// Returns an ItemType that is used to get correct properties schema/rule for given node
        /// </summary>
        string ItemType { get; }

        /// <summary>
        /// Adds a child to the node
        /// </summary>
        /// <param name="childNode">child node to be added. Should not be null.</param>
        void AddChild(IDependencyNode childNode);

        /// <summary>
        /// Removes child from node's children.
        /// </summary>
        /// <param name="childNode">Node to be removed if it belongs to children. Should not be null.</param>
        void RemoveChild(IDependencyNode childNode);
    }
}
