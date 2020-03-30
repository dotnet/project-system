// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Host, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ITargetFrameworkProvider
    {
        /// <summary>
        /// Parses full tfm or short framework name and returns a corresponding <see cref="ITargetFramework"/>
        /// instance, or <see langword="null" /> if the framework name has an invalid format.
        /// </summary>
        ITargetFramework? GetTargetFramework(string? shortOrFullName);
    }
}
