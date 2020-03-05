// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal interface IDependenciesViewModelFactory
    {
        /// <summary>
        /// Returns a view model for a node that represents a target framework.
        /// </summary>
        IDependencyViewModel CreateTargetViewModel(ITargetFramework targetFramework, bool hasReachableVisibleUnresolvedDependency);

        /// <summary>
        /// Returns a view model for a node that groups dependencies from a given provider.
        /// </summary>
        IDependencyViewModel? CreateGroupNodeViewModel(string providerType, bool hasReachableVisibleUnresolvedDependency);

        /// <summary>
        /// Returns the icon to use for the "Dependencies" root node.
        /// </summary>
        ImageMoniker GetDependenciesRootIcon(bool hasReachableVisibleUnresolvedDependency);
    }
}
