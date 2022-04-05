// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    /// <summary>
    /// Global service that reads and caches the contents of framework references.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Extension, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ITargetFrameworkContentCache
    {
        /// <summary>
        /// Returns an array of framework reference assembly items, lazily loading them on the first invocation.
        /// </summary>
        ImmutableArray<FrameworkReferenceAssemblyItem> GetContents(FrameworkReferenceIdentity framework);
    }
}
