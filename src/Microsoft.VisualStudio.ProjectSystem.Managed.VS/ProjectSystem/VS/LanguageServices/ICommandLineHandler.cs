// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes within the command-line, and applies them to a
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface ICommandLineHandler : IWorkspaceUpdateHandler
    {
        /// <summary>
        ///     Handles the specified added and removed command-line arguments, and applies
        ///     them to the underlying <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="IWorkspaceProjectContext"/> to update.
        /// </param>
        /// <param name="version">
        ///     An <see cref="IComparable"/> representing the <see cref="ConfiguredProject.ProjectVersion"/> at
        ///     the time the <see cref="BuildOptions"/> were produced.
        /// </param>
        /// <param name="added">
        ///     A <see cref="BuildOptions"/> representing the added arguments.
        /// </param>
        /// <param name="removed">
        ///     A <see cref="BuildOptions"/> representing the removed arguments.
        /// </param>
        /// <param name="state">
        ///     A <see cref="ContextState"/> describing the state of the <see cref="IWorkspaceProjectContext"/>.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IManagedProjectDiagnosticOutputService"/> for logging to the log.
        /// </param>
        void Handle(IWorkspaceProjectContext context, IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IManagedProjectDiagnosticOutputService logger);
    }
}
