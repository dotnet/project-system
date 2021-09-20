// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;AdditionalFiles/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class AdditionalFilesItemHandler : AbstractWorkspaceContextHandler, ICommandLineHandler
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

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IProjectDiagnosticOutputService logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                RemoveFromContextIfPresent(fullPath, logger);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                AddToContextIfNotPresent(fullPath, state.IsActiveEditorContext, logger);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, bool isActiveContext, IProjectDiagnosticOutputService logger)
        {
            if (!_paths.Contains(fullPath))
            {
                logger.WriteLine("Adding additional file '{0}'", fullPath);
                Context.AddAdditionalFile(fullPath, isActiveContext);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(string fullPath, IProjectDiagnosticOutputService logger)
        {
            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing additional file '{0}'", fullPath);
                Context.RemoveAdditionalFile(fullPath);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }
    }
}
