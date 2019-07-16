// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Internal extension of <see cref="IProjectDependenciesSubTreeProvider"/> contract,
    /// to support generic dependencies modifications.
    /// </summary>
    internal interface IProjectDependenciesSubTreeProviderInternal : IProjectDependenciesSubTreeProvider
    {
        ImageMoniker ImplicitIcon { get; }
    }
}
