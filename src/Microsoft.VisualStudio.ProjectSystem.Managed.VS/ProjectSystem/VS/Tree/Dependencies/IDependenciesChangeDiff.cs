// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public interface IDependenciesChangeDiff
    {
        IEnumerable<IDependencyNode> AddedNodes { get; }
        IEnumerable<IDependencyNode> RemovedNodes { get; }
    }
}
