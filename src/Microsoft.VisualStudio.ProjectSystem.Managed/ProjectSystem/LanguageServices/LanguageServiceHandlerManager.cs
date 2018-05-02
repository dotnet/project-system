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
        private readonly UnconfiguredProject _project;
        private readonly ICommandLineParserService _commandLineParser;
        private readonly IContextHandlerProvider _handlerProvider;
        private readonly IProjectLogger _logger;

        [ImportingConstructor]
        public LanguageServiceHandlerManager(UnconfiguredProject project, ICommandLineParserService commandLineParser, IContextHandlerProvider handlerProvider, IProjectLogger logger)
        {
            _project = project;
            _commandLineParser = commandLineParser;
            _handlerProvider = handlerProvider;
            _logger = logger;
            CommandLineNotifications = new OrderPrecedenceImportCollection<Action<string, BuildOptions, BuildOptions>>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<Action<string, BuildOptions, BuildOptions>> CommandLineNotifications
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
            ImmutableArray<(IEvaluationHandler handler, string evaluationRuleName)> handlers = _handlerProvider.GetEvaluationHandlers(context);

            IComparable version = update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

            using (IProjectLoggerBatch logger = _logger.BeginBatch())
            {
                WriteHeader(logger, update, version, RuleHandlerType.Evaluation, isActiveContext);

                foreach ((IEvaluationHandler handler, string evaluationRuleName) handler in handlers)
                {
                    string ruleName = handler.evaluationRuleName;

                    WriteRuleHeader(logger, ruleName);

                    IProjectChangeDescription projectChange = update.Value.ProjectChanges[ruleName];
                    if (projectChange.Difference.AnyChanges)
                    {
                        handler.handler.Handle(version, projectChange, isActiveContext, logger);
                    }
                    else
                    {
                        WriteRuleHasNoChanges(logger);
                    }

                    WriteRuleFooter(logger, ruleName);
                }

                WriteFooter(logger, update);
            }
        }

        private void HandleDesignTime(IProjectVersionedValue<IProjectSubscriptionUpdate> update, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Assumes.False(update.Value.ProjectChanges.Count == 0, "CPS should never send us an empty design-time build data.");

            IComparable version = update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

            using (IProjectLoggerBatch logger = _logger.BeginBatch())
            {
                string ruleName = CompilerCommandLineArgs.SchemaName;

                WriteHeader(logger, update, version, RuleHandlerType.DesignTimeBuild, isActiveContext);
                WriteRuleHeader(logger, ruleName);

                IProjectChangeDescription projectChange = update.Value.ProjectChanges[ruleName];

                // If nothing changed (even another failed design-time build), don't do anything
                if (projectChange.Difference.AnyChanges)
                {
                    ProcessOptions(projectChange, context, logger);
                    ProcessItems(version, projectChange, context, isActiveContext, logger);
                    ProcessDesignTimeBuildFailure(projectChange, context, logger);
                }
                else
                {
                    WriteRuleHasNoChanges(logger);
                }

                WriteRuleFooter(logger, ruleName);
                WriteFooter(logger, update);
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

        private static void ProcessDesignTimeBuildFailure(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, IProjectLogger logger)
        {
            // If 'CompileDesignTime' didn't run due to a preceeding failed target, or a failure in itself, CPS will send us an empty IProjectChangeDescription
            // that represents as if 'CompileDesignTime' ran but returned zero results.
            //
            // We still forward those 'removes' of references, sources, etc onto Roslyn to avoid duplicate/incorrect results when the next
            // successful build occurs, because it will be diff between it and this failed build.

            bool succeeded = projectChange.After.Items.Count > 0;

            if (context.LastDesignTimeBuildSucceeded != succeeded)
            {
                WriteDesignTimeBuildSuccess(logger, succeeded);
                context.LastDesignTimeBuildSucceeded = succeeded;
            }
        }

        private static void ProcessOptions(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, IProjectLogger logger)
        {
            // We don't pass differences to Roslyn for options, we just pass them all
            string commandlineArguments = string.Join(" ", projectChange.After.Items.Keys);

            WriteCommandLineArguments(logger, commandlineArguments);
            context.SetOptions(commandlineArguments);
        }

        private void ProcessItems(IComparable version, IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext, IProjectLogger logger)
        {
            ImmutableArray<ICommandLineHandler> handlers = _handlerProvider.GetCommandLineHandlers(context);

            BuildOptions addedItems = _commandLineParser.Parse(projectChange.Difference.AddedItems);
            BuildOptions removedItems = _commandLineParser.Parse(projectChange.Difference.RemovedItems);

            foreach (ICommandLineHandler handler in handlers)
            {
                handler.Handle(version, addedItems, removedItems, isActiveContext, logger);
            }

            CommandLineNotifications.FirstOrDefault()?.Value.Invoke(context.BinOutputPath, addedItems, removedItems);
        }

        private void WriteHeader(IProjectLoggerBatch logger, IProjectVersionedValue<IProjectSubscriptionUpdate> update, IComparable version, RuleHandlerType source, bool isActiveContext)
        {
            logger.WriteLine();
            logger.WriteLine("Processing language service changes for '{0}' [{1}]...", _project.FullPath, update.Value.ProjectConfiguration.Name);
            logger.WriteLine("Version:         {0}", version);
            logger.WriteLine("Source:          {0}", source);
            logger.WriteLine("IsActiveContext: {0}", isActiveContext);
            logger.IndentLevel++;
        }

        private void WriteFooter(IProjectLoggerBatch logger, IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            logger.IndentLevel--;
            logger.WriteLine();
            logger.WriteLine("Finished language service changes for '{0}' [{1}]", _project.FullPath, update.Value.ProjectConfiguration.Name);
        }

        private static void WriteRuleHeader(IProjectLoggerBatch logger, string ruleName)
        {
            logger.WriteLine();
            logger.WriteLine("Processing rule '{0}'...", ruleName);
            logger.IndentLevel++;
        }

        private static void WriteRuleFooter(IProjectLoggerBatch logger, string ruleName)
        {
            logger.IndentLevel--;
        }

        private static void WriteRuleHasNoChanges(IProjectLoggerBatch logger)
        {
            logger.WriteLine("No changes.");
        }

        private static void WriteDesignTimeBuildSuccess(IProjectLogger logger, bool succeeded)
        {
            if (succeeded)
            {
                logger.WriteLine("Last design-time build suceeeded, turning semantic errors back on.");
            }
            else
            {
                logger.WriteLine("Last design-time build failed, turning semantic errors off.");
            }
        }

        private static void WriteCommandLineArguments(IProjectLogger logger, string commandLineArguments)
        {
            logger.WriteLine("Options: {0}", commandLineArguments);
        }
    }
}
