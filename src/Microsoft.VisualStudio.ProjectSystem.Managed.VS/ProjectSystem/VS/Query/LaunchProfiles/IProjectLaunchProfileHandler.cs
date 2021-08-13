// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="UnconfiguredProject"/>-level handler for retrieving Project Query API entities for Launch Profiles.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectLaunchProfileHandler
    {
        /// <summary>
        /// Returns entities representing all launch profiles in the project.
        /// </summary>
        Task<IEnumerable<IEntityValue>> RetrieveAllLaunchProfileEntitiesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ILaunchProfilePropertiesAvailableStatus requestedProperties);
        
        /// <summary>
        /// Returns the entity representing the launch profile specified by the given <paramref name="id"/>.
        /// </summary>
        Task<IEntityValue?> RetrieveLaunchProfileEntityAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id, ILaunchProfilePropertiesAvailableStatus requestedProperties);
    }
}
