// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Creates instances of <see cref="IPropertyPageQueryCache"/>.
    /// </summary>
    /// <remarks>
    /// This type exists largely to enable injected mock implementations of
    /// <see cref="IPropertyPageQueryCache" /> for unit testing purposes.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPropertyPageQueryCacheProvider
    {
        IPropertyPageQueryCache CreateCache(UnconfiguredProject project);
    }
}
