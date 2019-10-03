// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes within the command-line, and applies them to a 
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface ICommandLineHandler : IWorkspaceContextHandler
    {
        /// <summary>
        ///     Handles the specified added and removed command-line arguments, and applies 
        ///     them to the underlying <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
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
        /// <param name="isActiveContext">
        ///     <see langword="true"/> if the underlying <see cref="IWorkspaceProjectContext"/>
        ///     is the active context; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IProjectLogger"/> for logging to the log.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="version"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="added"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="removed"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger);
    }
}
