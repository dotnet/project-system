// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

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
        /// <param name="outputMessage">The message to write.</param>
        Task WriteLineAsync(string outputMessage);
    }
}
