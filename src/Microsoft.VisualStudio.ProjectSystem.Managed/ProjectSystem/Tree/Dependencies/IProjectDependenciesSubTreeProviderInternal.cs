// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
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
