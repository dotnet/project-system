// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class DependenciesChangeDiff : IDependenciesChangeDiff
    {
        public DependenciesChangeDiff(IEnumerable<IDependencyNode> addedNodes,
                                      IEnumerable<IDependencyNode> removedNodes)
        {
            AddedNodes = addedNodes;
            RemovedNodes = removedNodes;
        }

        public IEnumerable<IDependencyNode> AddedNodes { get; private set; }
        public IEnumerable<IDependencyNode> RemovedNodes { get; private set; }
    }
}
