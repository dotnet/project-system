// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// A service that is initialized when the VS package is initialized.
    /// </summary>
    /// <remarks>
    /// Implementations must be exported in either global or project service scopes.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore, ContractName = ExportContractNames.Scopes.ProjectService)]
    internal interface IPackageService
    {
        /// <summary>
        /// Called when the package is initializing.
        /// </summary>
        /// <remarks>
        /// Always called on the UI thread.
        /// </remarks>
        Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider);
    }
}
