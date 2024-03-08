// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides access to a <see cref="IWorkspaceProjectContext"/> and associated services.
    /// </summary>
    internal interface IWorkspace
    {
        /// <summary>
        ///     Gets an identifier that uniquely identifies the <see cref="IWorkspaceProjectContext"/> across a solution.
        /// </summary>
        string ContextId { get; }

        /// <summary>
        ///     Gets the <see cref="IWorkspaceProjectContext"/> that provides access to the language service.
        /// </summary>
        IWorkspaceProjectContext Context { get; }

        /// <summary>
        ///     Gets the language service build error reporter object.
        /// </summary>
        IVsLanguageServiceBuildErrorReporter2 ErrorReporter { get; }
    }
}
