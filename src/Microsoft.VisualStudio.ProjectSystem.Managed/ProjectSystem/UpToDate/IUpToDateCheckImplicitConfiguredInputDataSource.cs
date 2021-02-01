// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Project value data source for instances of <see cref="UpToDateCheckImplicitConfiguredInput"/>.
    /// </summary>
    /// <remarks>
    /// Links several CPS-provided data sources together, producing a snapshot of data that the
    /// up-to-date check will need in order to compute its result correctly.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUpToDateCheckImplicitConfiguredInputDataSource : IProjectValueDataSource<UpToDateCheckImplicitConfiguredInput>
    {
    }
}
