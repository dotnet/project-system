// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles <c>AdditionalFiles</c> items during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceUpdateHandler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class AdditionalFilesItemHandler : IWorkspaceUpdateHandler, ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate additional files, we remember what 
        // sent to them and avoid sending on duplicates.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private readonly HashSet<string> _paths = new(StringComparers.Paths);

        [ImportingConstructor]
        public AdditionalFilesItemHandler(UnconfiguredProject project)
        {
            _project = project;
        }

        public void Handle(IWorkspaceProjectContext context, IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IManagedProjectDiagnosticOutputService logger)
        {
            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                RemoveFromContextIfPresent(context, fullPath, logger);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                AddToContextIfNotPresent(context, fullPath, state.IsActiveEditorContext, logger);
            }
        }

        private void AddToContextIfNotPresent(IWorkspaceProjectContext context, string fullPath, bool isActiveContext, IManagedProjectDiagnosticOutputService logger)
        {
            if (!_paths.Contains(fullPath))
            {
                logger.WriteLine("Adding additional file '{0}'", fullPath);
                context.AddAdditionalFile(fullPath, isActiveContext);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(IWorkspaceProjectContext context, string fullPath, IManagedProjectDiagnosticOutputService logger)
        {
            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing additional file '{0}'", fullPath);
                context.RemoveAdditionalFile(fullPath);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }
    }
}
