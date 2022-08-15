// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

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
        ///     Gets an object that represents a host-specific error reporter.
        /// </summary>
        /// <remarks>
        ///     Within a Visual Studio host, this is typically an object implementing IVsLanguageServiceBuildErrorReporter2.
        /// </remarks>
        object HostSpecificErrorReporter { get; }
    }
}
