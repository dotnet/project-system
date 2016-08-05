// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public interface IDependenciesChangeDiff
    {
        IImmutableList<IDependencyNode> AddedNodes { get; }
        IImmutableList<IDependencyNode> UpdatedNodes { get; }
        IImmutableList<IDependencyNode> RemovedNodes { get; }
    }
}
