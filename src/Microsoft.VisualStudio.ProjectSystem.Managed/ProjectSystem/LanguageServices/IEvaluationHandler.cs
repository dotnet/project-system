// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes to a language service rule,  and applies them to a 
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface IEvaluationHandler
    {
        /// <summary>
        ///     Gets the evaluation rule this handler handles.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing the evaluation rule that this <see cref="IEvaluationHandler"/> 
        ///     handles.
        /// </value>
        string EvaluationRuleName
        {
            get;
        }

        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
        ///     to the given <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="projectChange">
        ///     A <see cref="IProjectChangeDescription"/> representing the set of 
        ///     changes made to the project.
        /// </param>
        /// <param name="context">
        ///     A <see cref="IWorkspaceProjectContext"/> to update.
        /// </param>
        /// <param name="isActiveContext">
        ///     Flag indicating if the given <paramref name="context"/> is the active project context.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="projectChange"/> is <see langword="null"/>.
        /// </exception>
        void Handle(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext);

        /// <summary>
        /// Handles clearing any state specific to the given context being released.
        /// </summary>
        /// <param name="context">
        ///     A <see cref="IWorkspaceProjectContext"/> being released.
        /// </param>
        void OnContextReleasedAsync(IWorkspaceProjectContext context);
    }
}
