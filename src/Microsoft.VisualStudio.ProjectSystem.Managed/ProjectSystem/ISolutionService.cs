// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// A global service that tracks whether solution-level state.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.OneOrZero)]
    internal interface ISolutionService
    {
        /// <summary>
        /// Gets whether the solution is being closed, which can be useful to avoid doing
        /// redundant work while tearing down the solution.
        /// </summary>
        bool IsSolutionClosing { get; }
    }
}
