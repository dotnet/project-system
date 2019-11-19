// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// A service that is initialized when the VS package is initialized.
    /// </summary>
    /// <remarks>
    /// Implementations must be exported in global scope.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
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
