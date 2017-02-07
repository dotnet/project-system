// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal interface IDependenciesTreeViewProvider
    {
        IProjectTree BuildTree(IProjectTree dependenciesTree, IDependenciesSnapshot snapshot);
        IProjectTree FindByPath(IProjectTree root, string path);
    }
}
