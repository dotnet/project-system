// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Extensions for IDependencyNode implementations
    /// </summary>
    public static class IDependencyNodeExtensions
    {
        /// <summary>
        /// Finds a node with given id (recusrive when needed)
        /// </summary>
        public static IDependencyNode FindNode(this IDependencyNode self, DependencyNodeId id, bool recursive = false)
        {
            IDependencyNode resultNode = null;
            foreach(var child in self.Children)
            {
                if (child.Id.Equals(id))
                {
                    resultNode = child;
                }
                else if (recursive)
                {
                    resultNode = FindNode(child, id, recursive);
                }

                if (resultNode != null)
                {
                    break;
                }
            }

            return resultNode;
        }
    }
}
