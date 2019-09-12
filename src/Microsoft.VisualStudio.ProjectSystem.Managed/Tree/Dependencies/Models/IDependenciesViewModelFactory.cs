// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal interface IDependenciesViewModelFactory
    {
        IDependencyViewModel CreateTargetViewModel(TargetedDependenciesSnapshot snapshot);
        IDependencyViewModel? CreateRootViewModel(string providerType, bool hasUnresolvedDependency);
        ImageMoniker GetDependenciesRootIcon(bool hasUnresolvedDependencies);
    }
}
