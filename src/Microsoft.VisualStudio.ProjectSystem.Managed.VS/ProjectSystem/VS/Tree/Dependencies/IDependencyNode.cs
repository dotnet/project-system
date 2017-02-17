// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    public interface IDependencyNode: IEquatable<IDependencyNode>
    {
        /// <summary>
        /// Unique id of the node, combine of ItemSpec, ItemType, ProviderType and when
        /// needed some unique token.
        /// </summary>
        DependencyNodeId Id { get; }

        /// <summary>
        /// Formal node name from tree perspective. By default it is equal to Caption,
        /// however some nodes might have add some data to Caption, like version etc. So 
        /// we need to have a way to avoid caprion parsing and get it directly form node.
        /// Note: it is different from DependencyNodeId.ItemSpec, since that one is unique 
        /// form project item perspective for default nodes and might have any form, not 
        /// necesarily readable. So actual IProjectTree would use Caption to display nodes,
        /// and Name or ItemSpec when tries to find children depending on node types.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Node's caption displayed in the tree
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Unique name constructed in the form Caption (ItemSpec).
        /// Is used to dedupe dependency nodes when they have same caption but different 
        /// ItemSpec (path)
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// Specifies if dependency associate for the node is resolved or not
        /// </summary>
        bool Resolved { get; }

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
        /// Changes some properties of the node
        /// </summary>
        void SetProperties(string caption = null);

        /// <summary>
        /// Children of current node.
        /// </summary>
        ImmutableHashSet<IDependencyNode> Children { get; }

        /// <summary>
        /// Quick check if node has children.
        /// </summary>
        bool HasChildren { get; }

        /// <summary>
        /// A list of node's properties that might be displayed in property pages
        /// (in BrowsableObject context).
        /// </summary>
        IImmutableDictionary<string, string> Properties { get; }

        /// <summary>
        /// Adds a child to the node
        /// </summary>
        /// <param name="childNode">child node to be added. Should not be null.</param>
        void AddChild(IDependencyNode childNode);

        /// <summary>
        /// Adds children to the node
        /// </summary>
        /// <param name="children">child nodes to be added. Should not be null.</param>
        void AddChildren(IEnumerable<IDependencyNode> children);

        /// <summary>
        /// Removes child from node's children.
        /// </summary>
        /// <param name="childNode">Node to be removed if it belongs to children. Should not be null.</param>
        void RemoveChild(IDependencyNode childNode);

        /// <summary>
        /// Removes all children from node
        /// </summary>
        void RemoveAllChildren();
    }
}
