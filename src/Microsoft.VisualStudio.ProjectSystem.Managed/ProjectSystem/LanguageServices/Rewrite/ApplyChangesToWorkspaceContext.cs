// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IApplyChangesToWorkspaceContext))]
    internal class ApplyChangesToWorkspaceContext : OnceInitializedOnceDisposed, IApplyChangesToWorkspaceContext
    {
        private readonly ConfiguredProject _project;
        private readonly ICommandLineParserService _commandLineParser;

        private IWorkspaceProjectContext _context;
        private ExportLifetimeContext<IWorkspaceContextHandler>[] _handlers;

        [ImportingConstructor]
        public ApplyChangesToWorkspaceContext(ConfiguredProject project, ICommandLineParserService commandLineParser)
        {
            _project = project;
            _commandLineParser = commandLineParser;

            HandlerFactories = new OrderPrecedenceExportFactoryCollection<IWorkspaceContextHandler>();
        }

        [ImportMany]
        public OrderPrecedenceExportFactoryCollection<IWorkspaceContextHandler> HandlerFactories
        {
            get;
        }

        public void Initialize(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            if (_context != null)
                throw new InvalidOperationException();

            _context = context;

            EnsureInitialized();
        }

        public void ApplyDesignTime(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool isActiveContext)
        {
            Requires.NotNull(update, nameof(update));

            if (!IsInitialized)
                throw new InvalidOperationException();

            Assumes.False(update.Value.ProjectChanges.Count == 0, "CPS should never send us an empty design-time build data.");

            IComparable version = update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

            IProjectChangeDescription projectChange = update.Value.ProjectChanges[CompilerCommandLineArgs.SchemaName];

            // If nothing changed (even another failed design-time build), don't do anything
            if (projectChange.Difference.AnyChanges)
            {
                ProcessOptions(projectChange);
                ProcessItems(version, projectChange, isActiveContext);
                ProcessDesignTimeBuildFailure(projectChange);
            }
        }

        public void ApplyEvaluation(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool isActiveContext)
        {
            Requires.NotNull(update, nameof(update));

            if (!IsInitialized)
                throw new InvalidOperationException();

            Assumes.False(update.Value.ProjectChanges.Count == 0, "CPS should never send us an empty evaluation data.");

            IComparable version = update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                if (handler.Value is IEvaluationHandler evaluationHandler)
                {
                    IProjectChangeDescription projectChange = update.Value.ProjectChanges[evaluationHandler.EvaluationRule];
                    if (projectChange.Difference.AnyChanges)
                    {
                        evaluationHandler.Handle(version, projectChange, isActiveContext, null);
                    }
                }
            }
        }

        public IEnumerable<string> GetEvaluationRules()
        {
            return _handlers.Select(e => e.Value)
                            .OfType<IEvaluationHandler>()
                            .Select(e => e.EvaluationRule)
                            .Distinct(StringComparers.RuleNames);
        }

        public IEnumerable<string> GetDesignTimeRules()
        {
            return new string[] { CompilerCommandLineArgs.SchemaName };
        }

        protected override void Dispose(bool disposing)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                handler.Dispose();
            }

            _context = null;
            _handlers = null;
        }

        protected override void Initialize()
        {
            _handlers = HandlerFactories.Select(h => h.CreateExport())
                                        .ToArray();

            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                handler.Value.Initialize(_context);
            }

            // By default, set "LastDesignTimeBuildSucceeded = false" to turn off diagnostics until first design time build succeeds for this project.
            _context.LastDesignTimeBuildSucceeded = false;
        }

        private void ProcessDesignTimeBuildFailure(IProjectChangeDescription projectChange)
        {
            // If 'CompileDesignTime' didn't run due to a preceeding failed target, or a failure in itself, CPS will send us an empty IProjectChangeDescription
            // that represents as if 'CompileDesignTime' ran but returned zero results.
            //
            // We still forward those 'removes' of references, sources, etc onto Roslyn to avoid duplicate/incorrect results when the next
            // successful build occurs, because it will be diff between it and this failed build.
            bool succeeded = projectChange.After.Items.Count > 0;

            if (_context.LastDesignTimeBuildSucceeded != succeeded)
            {
                _context.LastDesignTimeBuildSucceeded = succeeded;
            }
        }

        private void ProcessOptions(IProjectChangeDescription projectChange)
        {
            // We don't pass differences to Roslyn for options, we just pass them all
            string commandlineArguments = string.Join(" ", projectChange.After.Items.Keys);

            _context.SetOptions(commandlineArguments);
        }

        private void ProcessItems(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext)
        {
            BuildOptions addedItems = _commandLineParser.Parse(projectChange.Difference.AddedItems);
            BuildOptions removedItems = _commandLineParser.Parse(projectChange.Difference.RemovedItems);

            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                if (handler.Value is ICommandLineHandler commandLineHandler)
                {
                    commandLineHandler.Handle(version, addedItems, removedItems, isActiveContext, null);
                }
            }
        }
    }
}
