// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface ICrossTargetRuleHandler<T> where T : IRuleChangeContext
    {
        /// <summary>
        ///     Gets the rule this handler handles.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing the rule that this <see cref="ICrossTargetRuleHandler{T}"/> 
        ///     handles.
        /// </value>
        ImmutableHashSet<string> GetRuleNames(RuleHandlerType handlerType);

        /// <summary>
        ///     Gets a value indicating what type of change the handler handlers.
        /// </summary>
        /// <param name="handlerType">One of the <see cref="RuleHandlerType"/> values indicate the type of change the 
        ///     <see cref="ICrossTargetRuleHandler{T}"/> handles
        /// </param>
        /// <returns></returns>
        bool SupportsHandlerType(RuleHandlerType handlerType);

        /// <summary>
        ///     Gets a value indicating the handler should be invoked even for updates with no underlying project changes (e.g. broken design time builds).
        /// </summary>
        bool ReceiveUpdatesWithEmptyProjectChange { get; }

        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
        ///     to the given <see cref="ITargetedProjectContext"/>.
        /// </summary>
        Task HandleAsync(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot>> e, 
                         IImmutableDictionary<string, IProjectChangeDescription> projectChange, 
                         ITargetedProjectContext targetedProjectContext,
                         bool isActiveContext,
                         T ruleChangeContext);

        /// <summary>
        /// Handles clearing any state specific to the given context being released.
        /// </summary>
        /// <param name="context">
        ///     A <see cref="ITargetedProjectContext"/> being released.
        /// </param>
        Task OnContextReleasedAsync(ITargetedProjectContext context);

    }
}
