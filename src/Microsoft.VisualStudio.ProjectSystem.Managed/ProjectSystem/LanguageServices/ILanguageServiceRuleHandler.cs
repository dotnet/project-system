// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes to a language service rule,  and applies them to a 
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal interface ILanguageServiceRuleHandler
    {
        /// <summary>
        ///     Sets the context that the handler manipulates.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="IWorkspaceProjectContext"/> that the <see cref="ILanguageServiceRuleHandler"/> 
        ///     manipulates.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        void SetContext(IWorkspaceProjectContext context);

        /// <summary>
        ///     Gets the rule this handler handles.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> containing the rule that this <see cref="ILanguageServiceRuleHandler"/> 
        ///     handles.
        /// </value>
        string RuleName
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating what type of change the handler handlers.
        /// </summary>
        /// <value>
        ///     One of the <see cref="RuleHandlerType"/> values indicate the type of change the 
        ///     <see cref="ILanguageServiceRuleHandler"/> handles.
        /// </value>
        RuleHandlerType HandlerType
        {
            get;
        }

        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
        ///     to the <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="e">
        ///     A <see cref="IProjectVersionedValue{IProjectSubscriptionService}"/> representing 
        ///     the overall changes to the project.
        /// </param>
        /// <param name="projectChange">
        ///     A <see cref="IProjectChangeDescription"/> representing the set of 
        ///     changes made to the project.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="e"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="projectChange"/> is <see langword="null"/>.
        /// </exception>
        Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, IProjectChangeDescription projectChange);
    }
}
