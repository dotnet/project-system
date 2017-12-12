// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IContextHandlerProvider))]
    internal partial class ContextHandlerProvider : IContextHandlerProvider
    {
        private static readonly ImmutableArray<(HandlerFactory Factory, string EvaluationRuleName)> s_handlerFactories = CreateHandlerFactories();
        private static readonly ImmutableArray<string> s_allEvaluationRuleNames = GetEvaluationRuleNames();
        private readonly ConcurrentDictionary<IWorkspaceProjectContext, Handlers> _contextToHandlers = new ConcurrentDictionary<IWorkspaceProjectContext, Handlers>();
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public ContextHandlerProvider(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            _project = project;
        }

        public ImmutableArray<string> EvaluationRuleNames
        {
            get { return s_allEvaluationRuleNames; }
        }

        public ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> GetEvaluationHandlers(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            Handlers handlers = _contextToHandlers.GetOrAdd(context, CreateHandlers);

            return handlers.EvaluationHandlers;
        }

        public ImmutableArray<ICommandLineHandler> GetCommandLineHandlers(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            Handlers handlers = _contextToHandlers.GetOrAdd(context, CreateHandlers);

            return handlers.CommandLineHandlers;
        }

        public void ReleaseHandlers(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            _contextToHandlers.TryRemove(context, out _);
        }

        private Handlers CreateHandlers(IWorkspaceProjectContext context)
        {
            var evaluationHandlers = ImmutableArray.CreateBuilder<(IEvaluationHandler Value, string EvaluationRuleName)>(s_handlerFactories.Length);
            var commandLineHandlers = ImmutableArray.CreateBuilder<ICommandLineHandler>(s_handlerFactories.Length);

            foreach (var factory in s_handlerFactories)
            {
                object handler = factory.Factory(_project, context);

                // NOTE: Handlers can be both IEvaluationHandler and ICommandLineHandler
                if (handler is IEvaluationHandler evaluationHandler)
                {
                    evaluationHandlers.Add((evaluationHandler, factory.EvaluationRuleName));
                }

                if (handler is ICommandLineHandler commandLineHandler)
                {
                    commandLineHandlers.Add(commandLineHandler);
                }
            }

            return new Handlers(evaluationHandlers.ToImmutable(), commandLineHandlers.ToImmutable());
        }

        private static ImmutableArray<(HandlerFactory Factory, string EvaluationRuleName)>  CreateHandlerFactories()
        {
            return ImmutableArray.Create<(HandlerFactory Factory, string EvaluationRuleName)>(
            
            // Factory                                                                      EvalautionRuleName                  Description

            // Evaluation and Command-line
            ((project, context) => new SourceItemHandler(project, context),                 Compile.SchemaName),                // <Compile /> item

            // Evaluation only
            ((project, context) => new ProjectPropertiesItemHandler(context),               ConfigurationGeneral.SchemaName),   // <ProjectGuid>, <TargetPath> properties

            // Command-line only
            ((project, context) => new MetadataReferenceItemHandler(project, context),      null),                              // <ProjectReference />, <Reference /> items
            ((project, context) => new AnalyzerItemHandler(project, context),               null),                              // <Analyzer /> item
            ((project, context) => new AdditionalFilesItemHandler(project, context),        null)                               // <AdditionalFiles /> item
            );
        }

        private static ImmutableArray<string> GetEvaluationRuleNames()
        {
            return s_handlerFactories.Select(e => e.EvaluationRuleName)
                                   .Where(name => !string.IsNullOrEmpty(name))
                                   .Distinct(StringComparers.RuleNames)
                                   .ToImmutableArray();
        }

        private delegate object HandlerFactory(UnconfiguredProject project, IWorkspaceProjectContext context);
    }
}
