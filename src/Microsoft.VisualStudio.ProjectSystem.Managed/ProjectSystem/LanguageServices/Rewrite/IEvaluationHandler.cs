// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes to a language service rule, and applies them to a
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface IEvaluationHandler : IWorkspaceContextHandler
    {
        /// <summary>
        ///     Gets the evaluation rule that the <see cref="IEvaluationHandler"/> handles.
        /// </summary>
        string EvaluationRule
        {
            get;
        }

        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
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
        void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger);
    }
}
