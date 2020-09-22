// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// Represents the data source of source items that are design time inputs or shared design time inputs, and have changed
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDesignTimeInputsChangeTracker : IProjectValueDataSource<DesignTimeInputSnapshot>
    {
    }
}
