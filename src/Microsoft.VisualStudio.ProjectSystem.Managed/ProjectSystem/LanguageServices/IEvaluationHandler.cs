// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes to a language service rule, and applies them to a
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface IEvaluationHandler
    {
        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
        ///     to the underlying <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="projectChange">
        ///     A <see cref="IProjectChangeDescription"/> representing the set of 
        ///     changes made to the project.
        /// </param>
        /// <param name="isActiveContext">
        ///     <see langword="true"/> if the underlying <see cref="IWorkspaceProjectContext"/>
        ///     is the active context; otherwise, <see langword="false"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="projectChange"/> is <see langword="null"/>.
        /// </exception>
        void Handle(IProjectChangeDescription projectChange, bool isActiveContext);
    }
}
