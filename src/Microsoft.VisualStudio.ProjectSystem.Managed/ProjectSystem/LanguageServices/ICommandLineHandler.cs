// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes within the command-line, and applies them to a <see cref="IWorkspaceProjectContext"/> 
    ///     instance.
    /// </summary>
    internal interface ICommandLineHandler
    {
        /// <summary>
        ///     Handles the specified added and removed command-line arguments, and applies 
        ///     them to the given <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="added">
        ///     A <see cref="BuildOptions"/> representing the added arguments.
        /// </param>
        /// <param name="removed">
        ///     A <see cref="BuildOptions"/> representing the removed arguments.
        /// </param>
        /// <param name="context">
        ///     A <see cref="IWorkspaceProjectContext"/> to update.
        /// </param>
        /// <param name="isActiveContext">
        ///     Flag indicating if the given <paramref name="context"/> is the active project context.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="added"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="removed"/> is <see langword="null"/>.
        /// </exception>
        void Handle(BuildOptions added, BuildOptions removed, IWorkspaceProjectContext context, bool isActiveContext);
    }
}
