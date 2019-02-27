// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Notifies consumers of the project system that the <see cref="IWorkspaceProjectContext"/>
    ///     has been made up-to-date with the project changes.
    /// </summary>
    internal interface IProjectUpdatedHandler : IWorkspaceContextHandler
    {
        /// <summary>
        ///     Gets the project rule that the <see cref="IProjectUpdatedHandler"/> handles.
        /// </summary>
        string ProjectUpdatedRule { get; }

        /// <summary>
        ///     Notifies consumers of the project system that the <see cref="IWorkspaceProjectContext"/>
        ///     has been made up-to-date with the project changes..
        /// </summary>
        /// <param name="version">
        ///     An <see cref="IComparable"/> representing the <see cref="ConfiguredProject.ProjectVersion"/> at
        ///     the time the <see cref="IProjectChangeDescription"/> was produced.
        /// </param>
        /// <param name="projectChange">
        ///     A <see cref="IProjectChangeDescription"/> representing the set of
        ///     changes made to the project.
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
        ///     <paramref name="projectChange"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        Task HandleUpdateAsync(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger);
    }
}
