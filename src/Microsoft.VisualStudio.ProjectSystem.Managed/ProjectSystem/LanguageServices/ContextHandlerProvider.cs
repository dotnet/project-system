// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IContextHandlerProvider))]
    internal partial class ContextHandlerProvider : IContextHandlerProvider
    {
        private readonly ConcurrentDictionary<IWorkspaceProjectContext, Lazy<Handlers>> _contextToHandlers = new ConcurrentDictionary<IWorkspaceProjectContext, Lazy<Handlers>>();
        private readonly Lazy<ImmutableArray<string>> _evalutionRuleNames;

        [ImportingConstructor]
        public ContextHandlerProvider(UnconfiguredProject project)
        {
            _evalutionRuleNames = new Lazy<ImmutableArray<string>>(GetEvaluationRuleNames);

            HandlerFactories = new OrderPrecedenceExportFactoryCollection<AbstractContextHandler, IContextHandlerMetadata>();
        }

        [ImportMany]
        public OrderPrecedenceExportFactoryCollection<AbstractContextHandler, IContextHandlerMetadata> HandlerFactories
        {
            get;
        }

        public ImmutableArray<string> EvaluationRuleNames
        {
            get { return _evalutionRuleNames.Value; }
        }

        public ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> GetEvaluationHandlers(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            Handlers handlers = GetHandlers(context);

            return handlers.EvaluationHandlers;
        }

        public ImmutableArray<ICommandLineHandler> GetCommandLineHandlers(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            Handlers handlers = GetHandlers(context);

            return handlers.CommandLineHandlers;
        }

        public void ReleaseHandlers(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            if (_contextToHandlers.TryRemove(context, out Lazy<Handlers> handlers))
            {
                foreach (var handler in handlers.Value.All)
                {
                    handler.Dispose();
                }
            }
        }

        private Handlers GetHandlers(IWorkspaceProjectContext context)
        {
            // We wrap creation in a Lazy<T> to avoid creating two sets of handlers
            // in a race, one of which would need to be cleaned up to avoid leaking.
            // The handlers don't actually get created until Value is accessed.
            Lazy<Handlers> handler = _contextToHandlers.GetOrAdd(context, CreateHandlers);

            return handler.Value;
        }

        private Lazy<Handlers> CreateHandlers(IWorkspaceProjectContext context)
        {
            var handlers = ImmutableArray.CreateBuilder<ExportLifetimeContext<AbstractContextHandler>>(HandlerFactories.Count);
            var evaluationHandlers = ImmutableArray.CreateBuilder<(IEvaluationHandler Value, string EvaluationRuleName)>(HandlerFactories.Count);
            var commandLineHandlers = ImmutableArray.CreateBuilder<ICommandLineHandler>(HandlerFactories.Count);

            foreach (ExportFactory<AbstractContextHandler, IContextHandlerMetadata> factory in HandlerFactories)
            {
                ExportLifetimeContext<AbstractContextHandler> handlerContext = factory.CreateExport();
                handlers.Add(handlerContext);

                // Handlers can be both IEvaluationHandler and ICommandLineHandler
                if (handlerContext.Value is IEvaluationHandler evaluationHandler)
                {
                    evaluationHandlers.Add((evaluationHandler, factory.Metadata.EvaluationRuleName));
                }

                if (handlerContext.Value is ICommandLineHandler commandLineHandler)
                {
                    commandLineHandlers.Add(commandLineHandler);
                }

                handlerContext.Value.Initialize(context);
            }

            return new Lazy<Handlers>(() => new Handlers(handlers.MoveToImmutable(), evaluationHandlers.ToImmutable(), commandLineHandlers.ToImmutable()));
        }

        private ImmutableArray<string> GetEvaluationRuleNames()
        {
            IEnumerable<string> evaluationRuleNames = HandlerFactories.Select(factory => factory.Metadata.EvaluationRuleName)
                                                                      .Where(name => !string.IsNullOrEmpty(name))
                                                                      .Distinct(StringComparers.RuleNames);

            return ImmutableArray.CreateRange(evaluationRuleNames);
        }
    }
}
