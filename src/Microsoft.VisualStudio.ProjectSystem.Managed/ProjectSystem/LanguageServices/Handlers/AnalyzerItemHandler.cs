// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;Analyzer/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class AnalyzerItemHandler : ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate analyzers, we remember what 
        // sent to them and avoid sending on duplicates.

        // See: https://github.com/dotnet/project-system/issues/2230
        private readonly UnconfiguredProject _project;
        private IWorkspaceProjectContext _context;
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparers.Paths);

        [ImportingConstructor]
        public AnalyzerItemHandler(UnconfiguredProject project)
            : this(project, null)
        {
        }

        public AnalyzerItemHandler(UnconfiguredProject project, IWorkspaceProjectContext context)
        {
            Requires.NotNull(project, nameof(project));

            _project = project;
            _context = context;
        }

        public void Initialize(IWorkspaceProjectContext context)
        {
            _context = context;
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            foreach (CommandLineAnalyzerReference analyzer in removed.AnalyzerReferences)
            {
                string fullPath = _project.MakeRooted(analyzer.FilePath);

                RemoveFromContextIfPresent(fullPath, logger);
            }

            foreach (CommandLineAnalyzerReference analyzer in added.AnalyzerReferences)
            {
                string fullPath = _project.MakeRooted(analyzer.FilePath);

                AddToContextIfNotPresent(fullPath, logger);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, IProjectLogger logger)
        {
            if (!_paths.Contains(fullPath))
            {
                logger.WriteLine("Adding analyzer '{0}'", fullPath);
                _context.AddAnalyzerReference(fullPath);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(string fullPath, IProjectLogger logger)
        {
            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing analyzer '{0}'", fullPath);
                _context.RemoveAnalyzerReference(fullPath);

                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }
    }
}
