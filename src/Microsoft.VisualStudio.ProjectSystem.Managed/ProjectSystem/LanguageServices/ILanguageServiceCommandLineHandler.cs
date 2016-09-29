// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes within the command-line, and applies them to a <see cref="IWorkspaceProjectContext"/> 
    ///     instance.
    /// </summary>
    internal interface ILanguageServiceCommandLineHandler
    {
        /// <summary>
        ///     Handles the specified added and removed command-line arguments, and applies 
        ///     them to the given <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="added">
        ///     A <see cref="CommandLineArguments"/> representing the added arguments.
        /// </param>
        /// <param name="removed">
        ///     A <see cref="CommandLineArguments"/> representing the removed arguments.
        /// </param>
        /// <param name="context">
        ///     A <see cref="IWorkspaceProjectContext"/> to update.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="added"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="removed"/> is <see langword="null"/>.
        /// </exception>
        void Handle(CommandLineArguments added, CommandLineArguments removed, IWorkspaceProjectContext context);
    }
}
