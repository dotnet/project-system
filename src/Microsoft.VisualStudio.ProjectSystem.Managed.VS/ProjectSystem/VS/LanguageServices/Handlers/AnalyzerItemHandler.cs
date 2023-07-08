// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;Analyzer/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceUpdateHandler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class AnalyzerItemHandler : IWorkspaceUpdateHandler, ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate analyzers, we remember what 
        // sent to them and avoid sending on duplicates.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private readonly HashSet<string> _paths = new(StringComparers.Paths);

        [ImportingConstructor]
        public AnalyzerItemHandler(UnconfiguredProject project)
        {
            _project = project;
        }

        public void Handle(IWorkspaceProjectContext context, IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IManagedProjectDiagnosticOutputService logger)
        {
            foreach (CommandLineAnalyzerReference analyzer in removed.AnalyzerReferences)
            {
                string fullPath = _project.MakeRooted(analyzer.FilePath);

                RemoveFromContextIfPresent(context, fullPath, logger);
            }

            foreach (CommandLineAnalyzerReference analyzer in added.AnalyzerReferences)
            {
                string fullPath = _project.MakeRooted(analyzer.FilePath);

                AddToContextIfNotPresent(context, fullPath, logger);
            }
        }

        private void AddToContextIfNotPresent(IWorkspaceProjectContext context, string fullPath, IManagedProjectDiagnosticOutputService logger)
        {
            if (!_paths.Contains(fullPath))
            {
                logger.WriteLine("Adding analyzer '{0}'", fullPath);
                context.AddAnalyzerReference(fullPath);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(IWorkspaceProjectContext context, string fullPath, IManagedProjectDiagnosticOutputService logger)
        {
            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing analyzer '{0}'", fullPath);
                context.RemoveAnalyzerReference(fullPath);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }
    }
}
