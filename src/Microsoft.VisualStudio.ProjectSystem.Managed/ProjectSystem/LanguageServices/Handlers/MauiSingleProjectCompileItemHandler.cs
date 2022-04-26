// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;Compile/&gt; item during design-time builds.
    /// </summary>
    /// <remarks>
    ///     Unlike the <see cref="CompileItemHandler"/> (which is used everywhere _except_ MAUI
    ///     Single Project scenarios) this does not make any use of evaluation data, only
    ///     design-time build data. This way we don't have to worry about reconciling the two,
    ///     and avoids problems with files present in evaluation but removed by targets.
    /// </remarks>
    [Export(typeof(IWorkspaceContextHandler))]
    [AppliesTo(ProjectCapability.MauiSingleProject + " & " + ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class MauiSingleProjectCompileItemHandler : AbstractWorkspaceContextHandler, ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate compile items, we remember what 
        // sent to them and avoid sending on duplicates.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private readonly HashSet<string> _paths = new(StringComparers.Paths);

        [ImportingConstructor]
        public MauiSingleProjectCompileItemHandler(UnconfiguredProject project)
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

            foreach (CommandLineSourceFile additionalFile in removed.SourceFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                RemoveFromContextIfPresent(fullPath, logger);
            }

            foreach (CommandLineSourceFile additionalFile in added.SourceFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                AddToContextIfNotPresent(fullPath, state.IsActiveEditorContext, logger);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, bool isActiveContext, IProjectDiagnosticOutputService logger)
        {
            if (!_paths.Contains(fullPath))
            {
                string[]? folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(_project.FullPath), fullPath, ImmutableStringDictionary<string>.EmptyOrdinal);
                logger.WriteLine("Adding additional file '{0}'", fullPath);
                Context.AddSourceFile(fullPath, isActiveContext, folderNames);
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
