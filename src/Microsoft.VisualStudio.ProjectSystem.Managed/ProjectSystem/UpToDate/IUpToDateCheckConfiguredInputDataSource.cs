// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Project value data source for instances of <see cref="UpToDateCheckConfiguredInput"/>.
    /// </summary>
    /// <remarks>
    /// Aggregates data from implicitly active configurations via
    /// <see cref="IUpToDateCheckImplicitConfiguredInputDataSource"/>.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUpToDateCheckConfiguredInputDataSource : IProjectValueDataSource<UpToDateCheckConfiguredInput>
    {
    }
}
