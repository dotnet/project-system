// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     A configured-project service which will be activated when its configured project becomes implicitly active, or deactivated when it not.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IImplicitlyActiveService
    {
        /// <summary>
        ///     Activates the service.
        /// </summary>
        Task ActivateAsync();

        /// <summary>
        ///     Deactivates the service.
        /// </summary>
        Task DeactivateAsync();
    }
}
