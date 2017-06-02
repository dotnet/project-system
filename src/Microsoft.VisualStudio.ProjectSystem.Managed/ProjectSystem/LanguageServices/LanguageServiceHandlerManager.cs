// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export]
    internal class LanguageServiceHandlerManager
    {
        private readonly ICommandLineParserService _commandLineParser;
        private readonly IContextHandlerProvider _handlerProvider;
        private readonly IProjectLogger _logger;

        [ImportingConstructor]
        public LanguageServiceHandlerManager(UnconfiguredProject project, ICommandLineParserService commandLineParser, IContextHandlerProvider handlerProvider, IProjectLogger logger)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(commandLineParser, nameof(commandLineParser));
            Requires.NotNull(handlerProvider, nameof(handlerProvider));
            Requires.NotNull(logger, nameof(logger));

            _commandLineParser = commandLineParser;
            _handlerProvider = handlerProvider;
            _logger = logger;
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
            ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> handlers = _handlerProvider.GetEvaluationHandlers(context);

            using (IProjectLoggerBatch logger = _logger.BeginBatch())
            {
                IComparable version = update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

                foreach (var handler in handlers)
                {
                    IProjectChangeDescription projectChange = update.Value.ProjectChanges[handler.EvaluationRuleName];
                    if (projectChange.Difference.AnyChanges)
                    {
                        handler.Value.Handle(version, projectChange, isActiveContext, logger);
                    }
                }
            }
        }

        private void HandleDesignTime(IProjectVersionedValue<IProjectSubscriptionUpdate> update, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Assumes.False(update.Value.ProjectChanges.Count == 0, "CPS should never send us an empty design-time build data.");

            using (IProjectLoggerBatch logger = _logger.BeginBatch())
            {
                IProjectChangeDescription projectChange = update.Value.ProjectChanges[CompilerCommandLineArgs.SchemaName];
                IComparable version = update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

                // If nothing changed (even another failed design-time build), don't do anything
                if (projectChange.Difference.AnyChanges)
                {
                    ProcessDesignTimeBuildFailure(projectChange, context);
                    ProcessOptions(projectChange, context);
                    ProcessItems(version, projectChange, context, isActiveContext, logger);
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

            _handlerProvider.ReleaseHandlers(context);
        }

        public IEnumerable<string> GetWatchedRules(RuleHandlerType handlerType)
        {
            if (handlerType == RuleHandlerType.Evaluation)
            {
                return _handlerProvider.EvaluationRuleNames;
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

        private void ProcessItems(IComparable version, IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext, IProjectLogger logger)
        {
            ImmutableArray<ICommandLineHandler> handlers = _handlerProvider.GetCommandLineHandlers(context);

            BuildOptions addedItems = _commandLineParser.Parse(projectChange.Difference.AddedItems);
            BuildOptions removedItems = _commandLineParser.Parse(projectChange.Difference.RemovedItems);

            foreach (var handler in handlers)
            {
                handler.Handle(version, addedItems, removedItems, isActiveContext, logger);
            }
        }
    }
}
