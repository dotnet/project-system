// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers;

/// <summary>
///     Handles changes to Compile items during project evaluations and items that are passed
///     to the compiler during design-time builds.
/// </summary>
[Export(typeof(IWorkspaceUpdateHandler))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[method: ImportingConstructor]
internal class CompileItemHandler(UnconfiguredProject project) : AbstractEvaluationCommandLineHandler(project), IWorkspaceUpdateHandler, IProjectEvaluationHandler, ICommandLineHandler
{
    public string ProjectEvaluationRule => Compile.SchemaName;

    public void Handle(IWorkspaceProjectContext context, ProjectConfiguration projectConfiguration, IComparable version, IProjectChangeDescription projectChange, ContextState state, IManagedProjectDiagnosticOutputService logger)
    {
        ApplyProjectEvaluation(context, version, projectChange.Difference, projectChange.Before.Items, projectChange.After.Items, state.IsActiveEditorContext, logger);
    }

    public void Handle(IWorkspaceProjectContext context, IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IManagedProjectDiagnosticOutputService logger)
    {
        IProjectChangeDiff difference = ConvertToProjectDiff(added, removed);

        ApplyProjectBuild(context, version, difference, state.IsActiveEditorContext, logger);

        IProjectChangeDiff ConvertToProjectDiff(BuildOptions added, BuildOptions removed)
        {
            var addedSet = GetFilePaths(added).ToImmutableHashSet(StringComparers.Paths);
            var removedSet = GetFilePaths(removed).ToImmutableHashSet(StringComparers.Paths);

            return new ProjectChangeDiff(addedSet, removedSet);

            IEnumerable<string> GetFilePaths(BuildOptions options)
            {
                return options.SourceFiles.Select(f => Project.MakeRelative(f.Path));
            }
        }
    }

    protected override void AddToContext(IWorkspaceProjectContext context, string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IManagedProjectDiagnosticOutputService logger)
    {
        string[]? folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(Project.FullPath)!, fullPath, metadata);

        logger.WriteLine("Adding source file '{0}'", fullPath);
        context.AddSourceFile(fullPath, isInCurrentContext: isActiveContext, folderNames: folderNames);
    }

    protected override void RemoveFromContext(IWorkspaceProjectContext context, string fullPath, IManagedProjectDiagnosticOutputService logger)
    {
        logger.WriteLine("Removing source file '{0}'", fullPath);
        context.RemoveSourceFile(fullPath);
    }

    protected override void UpdateInContext(IWorkspaceProjectContext context, string fullPath, IImmutableDictionary<string, string> previousMetadata, IImmutableDictionary<string, string> currentMetadata, bool isActiveContext, IManagedProjectDiagnosticOutputService logger)
    {
        if (LinkMetadataChanged(previousMetadata, currentMetadata))
        {
            logger.WriteLine("Removing and then re-adding source file '{0}' to <Link> metadata changes", fullPath);
            RemoveFromContext(context, fullPath, logger);
            AddToContext(context, fullPath, currentMetadata, isActiveContext, logger);
        }

        static bool LinkMetadataChanged(IImmutableDictionary<string, string> previousMetadata, IImmutableDictionary<string, string> currentMetadata)
        {
            string previousLink = previousMetadata.GetValueOrDefault(Compile.LinkProperty, string.Empty);
            string currentLink = currentMetadata.GetValueOrDefault(Compile.LinkProperty, string.Empty);

            return previousLink != currentLink;
        }
    }
}
