// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers;
using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IContextHandlerProvider))]
    internal partial class ContextHandlerProvider : IContextHandlerProvider
    {
        private static readonly ImmutableArray<(HandlerFactory factory, string evaluationRuleName)> s_handlerFactories = CreateHandlerFactories();
        private static readonly ImmutableArray<string> s_allEvaluationRuleNames = GetEvaluationRuleNames();
        private readonly ConcurrentDictionary<IWorkspaceProjectContext, Handlers> _contextToHandlers = new ConcurrentDictionary<IWorkspaceProjectContext, Handlers>();
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public ContextHandlerProvider(UnconfiguredProject project)
        {
            _project = project;
        }

        public ImmutableArray<string> EvaluationRuleNames
        {
            get { return s_allEvaluationRuleNames; }
        }

        public ImmutableArray<(IProjectEvaluationHandler handler, string evaluationRuleName)> GetEvaluationHandlers(IWorkspaceProjectContext context)
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
            var evaluationHandlers = PooledArray<(IProjectEvaluationHandler handler, string evaluationRuleName)>.GetInstance();
            var commandLineHandlers = PooledArray<ICommandLineHandler>.GetInstance();

            foreach ((HandlerFactory factory, string evaluationRuleName) factory in s_handlerFactories)
            {
                IWorkspaceContextHandler handler = factory.factory(_project);
                handler.Initialize(context);

                // NOTE: Handlers can be both IEvaluationHandler and ICommandLineHandler
                if (handler is IProjectEvaluationHandler evaluationHandler)
                {
                    evaluationHandlers.Add((evaluationHandler, factory.evaluationRuleName));
                }

                if (handler is ICommandLineHandler commandLineHandler)
                {
                    commandLineHandlers.Add(commandLineHandler);
                }
            }

            return new Handlers(evaluationHandlers.ToImmutableAndFree(), commandLineHandlers.ToImmutableAndFree());
        }

        private static ImmutableArray<(HandlerFactory factory, string? evaluationRuleName)> CreateHandlerFactories()
        {
            return ImmutableArray.Create<(HandlerFactory factory, string? evaluationRuleName)>(

            // Factory                                             EvaluationRuleName                  Description

            // Evaluation and Command-line
            (project => new SourceItemHandler(project),            Compile.SchemaName),                // <Compile /> items

            // Evaluation only
            (project => new DynamicItemHandler(project),           Content.SchemaName),                // <Content Include="*.cshtml" />  items
            (project => new ProjectPropertiesItemHandler(project), ConfigurationGeneral.SchemaName),   // <ProjectGuid>, <TargetPath> properties

            // Command-line only
            (project => new MetadataReferenceItemHandler(project), null),                              // <ProjectReference />, <Reference /> items
            (project => new AnalyzerItemHandler(project),          null),                              // <Analyzer /> item
            (project => new AdditionalFilesItemHandler(project),   null)                               // <AdditionalFiles /> item
            );
        }

        private static ImmutableArray<string> GetEvaluationRuleNames()
        {
            return s_handlerFactories.Select(e => e.evaluationRuleName)
                                     .Where(name => !string.IsNullOrEmpty(name))
                                     .Distinct(StringComparers.RuleNames)
                                     .ToImmutableArray();
        }

        private delegate IWorkspaceContextHandler HandlerFactory(UnconfiguredProject project);
    }
}
