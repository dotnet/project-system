// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export]
    internal class LanguageServiceHandlerManager
    {
        [ImportingConstructor]
        public LanguageServiceHandlerManager(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            Handlers = new OrderPrecedenceImportCollection<IEvaluationHandler>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IEvaluationHandler> Handlers
        {
            get;
        }

        public void Handle(IProjectVersionedValue<IProjectSubscriptionUpdate> update, RuleHandlerType handlerType, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(update, nameof(update));

            var handlers = Handlers.Select(h => h.Value)
                                   .Where(h => h.HandlerType == handlerType);

            foreach (var handler in handlers)
            {
                IProjectChangeDescription projectChange = update.Value.ProjectChanges[handler.EvaluationRuleName];
                if (handler.ReceiveUpdatesWithEmptyProjectChange || projectChange.Difference.AnyChanges)
                {
                    handler.Handle(projectChange, context, isActiveContext);
                }
            }
        }

        /// <summary>
        ///     Handles clearing any state specific to the given context being released.
        /// </summary>
        /// <param name="context">
        ///     A <see cref="IWorkspaceProjectContext"/> being released.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        public void OnContextReleased(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            foreach (var handler in Handlers)
            {
                handler.Value.OnContextReleased(context);
            }
        }

        public IEnumerable<string> GetWatchedRules(RuleHandlerType handlerType)
        {
            return Handlers.Where(h => h.Value.HandlerType == handlerType)
                           .Select(h => h.Value.EvaluationRuleName)
                           .Distinct(StringComparers.RuleNames)
                           .ToArray();
        }
    }
}
