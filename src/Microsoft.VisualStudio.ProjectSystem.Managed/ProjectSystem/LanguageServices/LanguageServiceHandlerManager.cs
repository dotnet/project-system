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
        private readonly ICommandLineParserService _commandLineParser;

        [ImportingConstructor]
        public LanguageServiceHandlerManager(UnconfiguredProject project, ICommandLineParserService commandLineParser)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(commandLineParser, nameof(commandLineParser));

            _commandLineParser = commandLineParser;

            EvaluationHandlers = new OrderPrecedenceImportCollection<IEvaluationHandler>(projectCapabilityCheckProvider: project);
            CommandLineHandlers = new OrderPrecedenceImportCollection<ICommandLineHandler>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IEvaluationHandler> EvaluationHandlers
        {
            get;
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<ICommandLineHandler> CommandLineHandlers
        {
            get;
        }

        public void Handle(IProjectVersionedValue<IProjectSubscriptionUpdate> update, RuleHandlerType handlerType, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(update, nameof(update));
            Requires.NotNull(context, nameof(context));

            if (handlerType == RuleHandlerType.Evaluation)
            {
                HandleEvaluation(update, context, isActiveContext);
            }
            else
            {
                HandleDesignTime(update, context, isActiveContext);
            }
        }

        private void HandleEvaluation(IProjectVersionedValue<IProjectSubscriptionUpdate> update, IWorkspaceProjectContext context, bool isActiveContext)
        {
            foreach (var handler in EvaluationHandlers.Select(h => h.Value))
            {
                IProjectChangeDescription projectChange = update.Value.ProjectChanges[handler.EvaluationRuleName];
                if (projectChange.Difference.AnyChanges)
                {
                    handler.Handle(projectChange, context, isActiveContext);
                }
            }
        }

        private void HandleDesignTime(IProjectVersionedValue<IProjectSubscriptionUpdate> update, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Assumes.False(update.Value.ProjectChanges.Count == 0, "CPS should never send us an empty design-time build data.");

            IProjectChangeDescription projectChange = update.Value.ProjectChanges[CompilerCommandLineArgs.SchemaName];

            // If nothing changed (even another failed design-time build), don't do anything
            if (projectChange.Difference.AnyChanges)
            {   
                ProcessDesignTimeBuildFailure(projectChange, context);
                ProcessOptions(projectChange, context);
                ProcessItems(projectChange, context, isActiveContext);
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

            foreach (var handler in EvaluationHandlers)
            {
                handler.Value.OnContextReleased(context);
            }
        }

        public IEnumerable<string> GetWatchedRules(RuleHandlerType handlerType)
        {
            if (handlerType == RuleHandlerType.Evaluation)
            {
                return EvaluationHandlers.Select(h => h.Value.EvaluationRuleName)
                                         .Distinct(StringComparers.RuleNames)
                                         .ToArray();
            }

            return new string[] { CompilerCommandLineArgs.SchemaName };
        }

        private void ProcessDesignTimeBuildFailure(IProjectChangeDescription projectChange, IWorkspaceProjectContext context)
        {
            // If 'CompileDesignTime' didn't run due to a preceeding failed target, or a failure in itself, CPS will send us an empty IProjectChangeDescription
            // that represents as if 'CompileDesignTime' ran but returned zero results.
            //
            // We still forward those 'removes' of references, sources, etc onto Roslyn to avoid duplicate/incorrect results when the next
            // successful build occurs, because it will be diff between it and this failed build.

            context.LastDesignTimeBuildSucceeded = projectChange.After.Items.Count > 0;
        }

        private static void ProcessOptions(IProjectChangeDescription projectChange, IWorkspaceProjectContext context)
        {
            // We don't pass differences to Roslyn for options, we just pass them all
            IEnumerable<string> commandlineArguments = projectChange.After.Items.Keys;
            context.SetOptions(string.Join(" ", commandlineArguments));
        }

        private void ProcessItems(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext)
        {
            BuildOptions addedItems = _commandLineParser.Parse(projectChange.Difference.AddedItems);
            BuildOptions removedItems = _commandLineParser.Parse(projectChange.Difference.RemovedItems);

            foreach (var handler in CommandLineHandlers)
            {
                handler.Value.Handle(addedItems, removedItems, context, isActiveContext);
            }
        }
    }
}
