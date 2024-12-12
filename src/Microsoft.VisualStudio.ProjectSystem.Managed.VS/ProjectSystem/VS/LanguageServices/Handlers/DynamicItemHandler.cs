// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers;

/// <summary>
///     Handles changes to dynamic items, such as Razor CSHTML files.
/// </summary>
[Export(typeof(IWorkspaceUpdateHandler))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[method: ImportingConstructor]
internal class DynamicItemHandler(UnconfiguredProject project) : IWorkspaceUpdateHandler, ISourceItemsHandler
{
    private const string RazorPagesExtension = ".cshtml";
    private const string RazorComponentsExtension = ".razor";

    private readonly HashSet<string> _paths = new(StringComparers.Paths);

    public void Handle(IWorkspaceProjectContext context, IImmutableDictionary<string, IProjectChangeDescription> projectChanges, ContextState state, IManagedProjectDiagnosticOutputService logger)
    {
        foreach ((_, IProjectChangeDescription projectChange) in projectChanges)
        {
            if (!projectChange.Difference.AnyChanges)
                continue;

            IProjectChangeDiff difference = projectChange.Difference.NormalizeRenames();

            foreach (string includePath in difference.RemovedItems)
            {
                if (IsDynamicFile(includePath))
                {
                    RemoveFromContextIfPresent(includePath);
                }
            }

            foreach (string includePath in difference.AddedItems)
            {
                if (IsDynamicFile(includePath))
                {
                    IImmutableDictionary<string, string> metadata = projectChange.After.Items.GetValueOrDefault(includePath, ImmutableStringDictionary<string>.EmptyOrdinal);

                    AddToContextIfNotPresent(includePath, metadata);
                }
            }

            // We Remove then Add changed items to pick up the Linked metadata
            foreach (string includePath in difference.ChangedItems)
            {
                if (IsDynamicFile(includePath))
                {
                    IImmutableDictionary<string, string> metadata = projectChange.After.Items.GetValueOrDefault(includePath, ImmutableStringDictionary<string>.EmptyOrdinal);

                    RemoveFromContextIfPresent(includePath);
                    AddToContextIfNotPresent(includePath, metadata);
                }
            }
        }

        void AddToContextIfNotPresent(string includePath, IImmutableDictionary<string, string> metadata)
        {
            string fullPath = project.MakeRooted(includePath);

            if (!_paths.Contains(fullPath))
            {
                string[]? folderNames = FileItemServices.GetLogicalFolderNames(project.GetProjectDirectory(), fullPath, metadata);

                logger.WriteLine("Adding dynamic file '{0}'", fullPath);
                context.AddDynamicFile(fullPath, folderNames);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        void RemoveFromContextIfPresent(string includePath)
        {
            string fullPath = project.MakeRooted(includePath);

            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing dynamic file '{0}'", fullPath);
                context.RemoveDynamicFile(fullPath);

                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }

        static bool IsDynamicFile(string includePath)
        {
            // Note a file called just '.cshtml' is still considered a Razor file
            return includePath.EndsWith(RazorPagesExtension, StringComparisons.Paths) ||
                   includePath.EndsWith(RazorComponentsExtension, StringComparisons.Paths);
        }
    }
}
