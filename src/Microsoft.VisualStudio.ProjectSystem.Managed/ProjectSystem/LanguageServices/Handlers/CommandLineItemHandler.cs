// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the command-line arguments that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceRuleHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class CommandLineItemHandler : AbstractLanguageServiceRuleHandler
    {
        private readonly ICommandLineParserService _commandLineParser;
        
        [ImportingConstructor]
        public CommandLineItemHandler(UnconfiguredProject project, ICommandLineParserService commandLineParser)
        {
            Requires.NotNull(commandLineParser, nameof(commandLineParser));

            _commandLineParser = commandLineParser;
            Handlers = new OrderPrecedenceImportCollection<ILanguageServiceCommandLineHandler>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<ILanguageServiceCommandLineHandler> Handlers
        {
            get;
        }

        public override string RuleName
        {
            get { return CompilerCommandLineArgs.SchemaName; }
        }

        public override RuleHandlerType HandlerType
        {
            get { return RuleHandlerType.DesignTimeBuild; }
        }

        // Broken design time builds generates updates with no changes.
        public override bool ReceiveUpdatesWithEmptyProjectChange => true;

        public override void Handle(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext, ProjectLoggerContext loggerContext)
        {
            Requires.NotNull(projectChange, nameof(projectChange));

            if (!ProcessDesignTimeBuildFailure(projectChange, context, loggerContext))
            {
                ProcessOptions(projectChange, context, loggerContext);
                ProcessItems(projectChange, context, isActiveContext, loggerContext);
            }
        }

        private static bool ProcessDesignTimeBuildFailure(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, ProjectLoggerContext loggerContext)
        {
            // WORKAROUND: https://github.com/dotnet/roslyn-project-system/issues/478
            // Check if the design-time build failed, if we have no arguments, then that is likely the 
            // case and we should ignore the results.

            bool designTimeBuildFailed = projectChange.After.Items.Count == 0;
            loggerContext.WriteLine("Setting 'last design time build succeeded' to {0}", designTimeBuildFailed);
            context.LastDesignTimeBuildSucceeded = !designTimeBuildFailed;
            return designTimeBuildFailed;
        }

        private static void ProcessOptions(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, ProjectLoggerContext loggerContext)
        {
            // We don't pass differences to Roslyn for options, we just pass them all
            IEnumerable<string> commandlineArguments = projectChange.After.Items.Keys;

            string commandLine = string.Join(" ", commandlineArguments);

            loggerContext.WriteLine("Setting options {0}", commandLine);
            context.SetOptions(commandLine);
        }

        private void ProcessItems(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext, ProjectLoggerContext loggerContext)
        {
            BuildOptions addedItems = _commandLineParser.Parse(projectChange.Difference.AddedItems);
            BuildOptions removedItems = _commandLineParser.Parse(projectChange.Difference.RemovedItems);

            foreach (var handler in Handlers)
            {
                handler.Value.Handle(addedItems, removedItems, context, isActiveContext, loggerContext);
            }
        }
    }
}
