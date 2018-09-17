// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Applies <see cref="IProjectVersionedValue{T}"/> values to a <see cref="IWorkspaceProjectContext"/>.
    /// </summary>
    [Export(typeof(IApplyChangesToWorkspaceContext))]
    internal class ApplyChangesToWorkspaceContext : OnceInitializedOnceDisposed, IApplyChangesToWorkspaceContext
    {
        private const string ProjectBuildRuleName = CompilerCommandLineArgs.SchemaName;
        private readonly ConfiguredProject _project;
        private readonly IProjectLogger _logger;
        private readonly ExportFactory<IWorkspaceContextHandler>[] _workspaceContextHandlerFactories;
        private IWorkspaceProjectContext _context;
        private ExportLifetimeContext<IWorkspaceContextHandler>[] _handlers;

        [ImportingConstructor]
        public ApplyChangesToWorkspaceContext(ConfiguredProject project, IProjectLogger logger, [ImportMany]ExportFactory<IWorkspaceContextHandler>[] workspaceContextHandlerFactories)
        {
            _project = project;
            _logger = logger;
            _workspaceContextHandlerFactories = workspaceContextHandlerFactories;

            CommandLineParsers = new OrderPrecedenceImportCollection<ICommandLineParserService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<ICommandLineParserService> CommandLineParsers
        {
            get;
        }

        public void Initialize(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            lock (SyncObject)
            {
                if (_context != null)
                    throw new InvalidOperationException("Already initialized.");

                _context = context;

                EnsureInitialized();
            }
        }

        public void ApplyProjectBuild(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool isActiveContext, CancellationToken cancellationToken)
        {
            Requires.NotNull(update, nameof(update));
            
            lock (SyncObject)
            {
                VerifyInitializedAndNotDisposed();

                IProjectChangeDescription projectChange = update.Value.ProjectChanges[ProjectBuildRuleName];

                if (projectChange.Difference.AnyChanges)
                {
                    IComparable version = GetConfiguredProjectVersion(update);

                    ProcessOptions(projectChange.After);
                    ProcessCommandLine(version, projectChange.Difference, isActiveContext, cancellationToken);
                    ProcessProjectBuildFailure(projectChange.After);
                }
            }
        }

        public void ApplyProjectEvaluation(IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool isActiveContext, CancellationToken cancellationToken)
        {
            Requires.NotNull(update, nameof(update));

            lock (SyncObject)
            {
                VerifyInitializedAndNotDisposed();

                IComparable version = GetConfiguredProjectVersion(update);

                ProcessProjectEvaluationHandlers(version, update, isActiveContext, cancellationToken);
            }
        }

        public IEnumerable<string> GetProjectEvaluationRules()
        {
            lock (SyncObject)
            {
                VerifyInitializedAndNotDisposed();

                return _handlers.Select(e => e.Value)
                                .OfType<IProjectEvaluationHandler>()
                                .Select(e => e.ProjectEvaluationRule)
                                .Distinct(StringComparers.RuleNames)
                                .ToArray();
            }
        }

        public IEnumerable<string> GetProjectBuildRules()
        {
            lock (SyncObject)
            {
                VerifyInitializedAndNotDisposed();

                return new string[] { ProjectBuildRuleName };
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_handlers != null)
            {
                foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
                {
                    handler.Dispose();
                }
            }

            _context = null;
            _handlers = null;
        }

        protected override void Initialize()
        {
            _handlers = _workspaceContextHandlerFactories.Select(h => h.CreateExport())
                                                         .ToArray();

            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                handler.Value.Initialize(_context);
            }
        }

        private void ProcessProjectBuildFailure(IProjectRuleSnapshot snapshot)
        {
            // If 'CompileDesignTime' didn't run due to a preceding failed target, or a failure in itself, IsEvaluationSucceeded returns false.
            //
            // We still forward those 'removes' of references, sources, etc onto Roslyn to avoid duplicate/incorrect results when the next
            // successful build occurs, because it will be diff between it and this failed build.
            _context.LastDesignTimeBuildSucceeded = snapshot.IsEvaluationSucceeded();
        }

        private void ProcessOptions(IProjectRuleSnapshot snapshot)
        {
            // We just pass all options to Roslyn
            string commandlineArguments = string.Join(" ", snapshot.Items.Keys);

            _context.SetOptions(commandlineArguments);
        }

        private void ProcessCommandLine(IComparable version, IProjectChangeDiff differences, bool isActiveContext, CancellationToken cancellationToken)
        {
            ICommandLineParserService parser = CommandLineParsers.FirstOrDefault()?.Value;

            Assumes.Present(parser);

            string baseDirectory = Path.GetDirectoryName(_project.UnconfiguredProject.FullPath);

            BuildOptions added = parser.Parse(differences.AddedItems, baseDirectory);
            BuildOptions removed = parser.Parse(differences.RemovedItems, baseDirectory);

            ProcessCommandLineHandlers(version, added, removed, isActiveContext, cancellationToken);
        }

        private void ProcessCommandLineHandlers(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, CancellationToken cancellationToken)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (handler.Value is ICommandLineHandler commandLineHandler)
                {
                    commandLineHandler.Handle(version, added, removed, isActiveContext, _logger);
                }
            }
        }

        private void ProcessProjectEvaluationHandlers(IComparable version, IProjectVersionedValue<IProjectSubscriptionUpdate> update, bool isActiveContext, CancellationToken cancellationToken)
        {
            foreach (ExportLifetimeContext<IWorkspaceContextHandler> handler in _handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (handler.Value is IProjectEvaluationHandler evaluationHandler)
                {
                    IProjectChangeDescription projectChange = update.Value.ProjectChanges[evaluationHandler.ProjectEvaluationRule];
                    if (!projectChange.Difference.AnyChanges)
                        continue;
                    
                    evaluationHandler.Handle(version, projectChange, isActiveContext, _logger);
                }
            }
        }

        private static IComparable GetConfiguredProjectVersion(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
        {
            return update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
        }

        private void VerifyInitializedAndNotDisposed()
        {
            VerifyNotDisposed();
            VerifyInitialized();
        }

        private void VerifyInitialized()
        {
            Verify.Operation(IsInitialized, "Must call Initialize(IWorkspaceProjectContext) first.");
        }

        private void VerifyNotDisposed()
        {
            Verify.NotDisposed(this);
        }
    }
}
