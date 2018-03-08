// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts a <see cref="IWorkspaceProjectContext"/> and handles the interaction between the project system and the language service.
    /// </summary>
    internal interface ILanguageServiceHost
    {
        /// <summary>
        ///     Gets an object that represents a host-specific error reporter.
        /// </summary>
        /// <value>
        ///     An <see cref="object"/> represent a host-specific error reporter, or <see langword="null"/> if it has not yet been initialized.
        /// </value>
        /// <remarks>
        ///     Within a Visual Studio host, this is typically an object implementing IVsLanguageServiceBuildErrorReporter2.
        /// </remarks>
        object HostSpecificErrorReporter
        {
            get;
        }

        /// <summary>
        ///     Gets the active workspace project context that provides access to the language service for the active configured project.
        /// </summary>
        /// <value>
        ///     An <see cref="IWorkspaceProjectContext"/> that provides access to the language service for the active configured project.
        /// </value>
        IWorkspaceProjectContext ActiveProjectContext
        {
            get;
        }

        /// <summary>
        ///     Gets the object that represents the host specific Edit and Continue Service.
        /// </summary>
        object HostSpecificEditAndContinueService
        {
            get;
        }

        /// <summary>
        ///     Initializes the langauge service host.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
