// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes to project evaluation rule and applies them to a
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface IProjectEvaluationHandler : IWorkspaceContextHandler
    {
        /// <summary>
        ///     Gets the project evaluation rule that the <see cref="IProjectEvaluationHandler"/> handles.
        /// </summary>
        string ProjectEvaluationRule { get; }

        /// <summary>
        ///     Handles the specified set of changes to the project evaluation rule, and applies them
        ///     to the underlying <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="version">
        ///     An <see cref="IComparable"/> representing the <see cref="ConfiguredProject.ProjectVersion"/> at
        ///     the time the <see cref="IProjectChangeDescription"/> was produced.
        /// </param>
        /// <param name="projectChange">
        ///     A <see cref="IProjectChangeDescription"/> representing the set of
        ///     changes made to the project.
        /// </param>
        /// <param name="state">
        ///     A <see cref="ContextState"/> describing the state of the <see cref="IWorkspaceProjectContext"/>.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IProjectDiagnosticOutputService"/> for logging to the log.
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
        void Handle(IComparable version, IProjectChangeDescription projectChange, ContextState state, IProjectDiagnosticOutputService logger);
    }
}
