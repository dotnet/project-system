// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Host, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ITargetFrameworkProvider
    {
        /// <summary>
        /// Returns a <see cref="TargetFramework"/> instance for the target framework moniker
        /// or <see langword="null" /> if the framework moniker is null or empty.
        /// </summary>
        TargetFramework? GetTargetFramework(string? targetFrameworkMoniker);
    }
}
