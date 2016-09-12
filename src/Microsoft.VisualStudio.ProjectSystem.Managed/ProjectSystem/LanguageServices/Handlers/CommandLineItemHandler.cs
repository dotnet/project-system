// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the command-line arguments that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceRuleHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService2)]
    internal class CommandLineItemHandler : ILanguageServiceRuleHandler
    {
        private readonly ICommandLineParserService _commandLineParser;
        private IWorkspaceProjectContext _context;

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

        public string RuleName
        {
            get { return CompilerCommandLineArgs.SchemaName; }
        }

        public RuleHandlerType HandlerType
        {
            get { return RuleHandlerType.DesignTimeBuild; }
        }

        public void SetContext(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            _context = context;

            foreach (var handler in Handlers)
            {
                handler.Value.SetContext(_context);
            }
        }

        public Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, IProjectChangeDescription projectChange)
        {
            Requires.NotNull(e, nameof(e));
            Requires.NotNull(projectChange, nameof(projectChange));

            ProcessOptions(projectChange);
            ProcessItems(projectChange);

            return Task.CompletedTask;
        }

        private void ProcessOptions(IProjectChangeDescription projectChange)
        {
            // We don't pass differences to Roslyn for options, we just pass them all
            IEnumerable<string> commandlineArguments = projectChange.After.Items.Keys;

            _context.SetOptions(string.Join(",", commandlineArguments));
        }

        private void ProcessItems(IProjectChangeDescription projectChange)
        {
            CommandLineArguments addedItems = _commandLineParser.Parse(projectChange.Difference.AddedItems);
            CommandLineArguments removedItems = _commandLineParser.Parse(projectChange.Difference.RemovedItems);

            foreach (var handler in Handlers)
            {
                handler.Value.Handle(addedItems, removedItems);
            }
        }
    }
}
