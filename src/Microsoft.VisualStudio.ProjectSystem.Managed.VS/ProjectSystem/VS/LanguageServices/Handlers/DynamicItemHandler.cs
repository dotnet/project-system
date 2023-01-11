// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to dynamic items, such as Razor CSHTML files.
    /// </summary>
    [Export(typeof(IWorkspaceUpdateHandler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class DynamicItemHandler : IWorkspaceUpdateHandler, ISourceItemsHandler
    {
        private const string RazorPagesExtension = ".cshtml";
        private const string RazorComponentsExtension = ".razor";
        private readonly UnconfiguredProject _project;
        private readonly HashSet<string> _paths = new(StringComparers.Paths);

        [ImportingConstructor]
        public DynamicItemHandler(UnconfiguredProject project)
        {
            _project = project;
        }

        public void Handle(IWorkspaceProjectContext context, IComparable version, IImmutableDictionary<string, IProjectChangeDescription> projectChanges, ContextState state, IManagedProjectDiagnosticOutputService logger)
        {
            foreach ((_, IProjectChangeDescription projectChange) in projectChanges)
            {
                if (!projectChange.Difference.AnyChanges)
                    continue;

                IProjectChangeDiff difference = HandlerServices.NormalizeRenames(projectChange.Difference);

                foreach (string includePath in difference.RemovedItems)
                {
                    if (IsDynamicFile(includePath))
                    {
                        RemoveFromContextIfPresent(context, includePath, logger);
                    }
                }

                foreach (string includePath in difference.AddedItems)
                {
                    if (IsDynamicFile(includePath))
                    {
                        IImmutableDictionary<string, string> metadata = projectChange.After.Items.GetValueOrDefault(includePath, ImmutableStringDictionary<string>.EmptyOrdinal);

                        AddToContextIfNotPresent(context, includePath, metadata, logger);
                    }
                }

                // We Remove then Add changed items to pick up the Linked metadata
                foreach (string includePath in difference.ChangedItems)
                {
                    if (IsDynamicFile(includePath))
                    {
                        IImmutableDictionary<string, string> metadata = projectChange.After.Items.GetValueOrDefault(includePath, ImmutableStringDictionary<string>.EmptyOrdinal);

                        RemoveFromContextIfPresent(context, includePath, logger);
                        AddToContextIfNotPresent(context, includePath, metadata, logger);
                    }
                }
            }
        }

        private void AddToContextIfNotPresent(IWorkspaceProjectContext context, string includePath, IImmutableDictionary<string, string> metadata, IManagedProjectDiagnosticOutputService logger)
        {
            string fullPath = _project.MakeRooted(includePath);

            if (!_paths.Contains(fullPath))
            {
                string[]? folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(_project.FullPath), fullPath, metadata);

                logger.WriteLine("Adding dynamic file '{0}'", fullPath);
                context.AddDynamicFile(fullPath, folderNames);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(IWorkspaceProjectContext context, string includePath, IManagedProjectDiagnosticOutputService logger)
        {
            string fullPath = _project.MakeRooted(includePath);

            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing dynamic file '{0}'", fullPath);
                context.RemoveDynamicFile(fullPath);

                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }

        private static bool IsDynamicFile(string includePath)
        {
            // Note a file called just '.cshtml' is still considered a Razor file
            return includePath.EndsWith(RazorPagesExtension, StringComparisons.Paths) ||
                   includePath.EndsWith(RazorComponentsExtension, StringComparisons.Paths);
        }
    }
}
