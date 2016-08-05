// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class DependenciesChange
    {
        public DependenciesChange()
        {
            AddedNodes = new List<IDependencyNode>();
            UpdatedNodes = new List<IDependencyNode>();
            RemovedNodes = new List<IDependencyNode>();
        }

        public List<IDependencyNode> AddedNodes { get; }
        public List<IDependencyNode> UpdatedNodes { get; }
        public List<IDependencyNode> RemovedNodes { get; }

        public IDependenciesChangeDiff GetDiff()
        {
            return new DependenciesChangeDiff(AddedNodes, UpdatedNodes, RemovedNodes);
        }

        private class DependenciesChangeDiff : IDependenciesChangeDiff
        {
            public DependenciesChangeDiff(IEnumerable<IDependencyNode> addedNodes,
                                          IEnumerable<IDependencyNode> updatedNodes,
                                          IEnumerable<IDependencyNode> removedNodes)
            {
                AddedNodes = addedNodes.ToImmutableList();
                UpdatedNodes = updatedNodes.ToImmutableList();
                RemovedNodes = removedNodes.ToImmutableList();
            }

            public IImmutableList<IDependencyNode> AddedNodes { get; }
            public IImmutableList<IDependencyNode> UpdatedNodes { get; }
            public IImmutableList<IDependencyNode> RemovedNodes { get; }
        }
    }
}
