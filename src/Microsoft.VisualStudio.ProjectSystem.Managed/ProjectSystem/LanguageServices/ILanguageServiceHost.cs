// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    }
}
