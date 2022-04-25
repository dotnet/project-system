// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    /// <summary>
    /// Defines a service to print out messages to the Hot Reload output window pane.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.System, Cardinality = Composition.ImportCardinality.OneOrZero)]
    internal interface IHotReloadDiagnosticOutputService
    {
        /// <summary>
        /// Writes a message to the Hot Reload diagnostic output window.
        /// </summary>
        /// <param name="hotReloadLogMessage">The message to write.</param>
        /// <param name="cancellationToken">The cancellation token to pass to the IHotReloadLogger</param>
        void WriteLine(HotReloadLogMessage hotReloadLogMessage, CancellationToken cancellationToken);
    }
}
