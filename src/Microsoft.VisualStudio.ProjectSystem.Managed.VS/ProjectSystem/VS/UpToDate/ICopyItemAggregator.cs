// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

/// <summary>
/// A global component that tracks the items each project contributes to the output directories of those projects that reference it, both directly and transitively.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface ICopyItemAggregator
{
    /// <summary>
    /// Stores the set of copy items produced by the calling project, and modelled
    /// in <paramref name="projectCopyData"/>.
    /// </summary>
    /// <param name="projectCopyData">The set of items to copy, and some details about the provider project.</param>
    void SetProjectData(ProjectCopyData projectCopyData);

    /// <summary>
    /// Walks the project reference graph in order to obtain all reachable copy items
    /// from the project identified by <paramref name="targetPath"/>.
    /// </summary>
    /// <remarks>
    /// We use the <c>TargetPath</c> property to identify projects, as that path takes
    /// other dimensions (such as target framework) into account.
    /// </remarks>
    /// <param name="targetPath">The target path of the project to query from.</param>
    /// <param name="logger">An object for writing log messages.</param>
    /// <returns>A structure containing the results of the operation.</returns>
    CopyItemsResult TryGatherCopyItemsForProject(string targetPath, BuildUpToDateCheck.Log logger);
}

/// <summary>
/// Results of gathering the items that must be copied as part of a project's build
/// by <see cref="ICopyItemAggregator.TryGatherCopyItemsForProject(string, BuildUpToDateCheck.Log)"/>.
/// </summary>
/// <param name="ItemsByProject">A sequence of items by project, that are reachable from the current project</param>
/// <param name="IsComplete">Indicates whether we have items from all reachable projects.</param>
/// <param name="TargetsWithoutReferenceAssemblies">
///     A list of target paths for projects that do not produce reference assemblies, or <see langword="null"/> if
///     all reachable projects do in fact produce reference assemblies.
/// </param>
internal record struct CopyItemsResult(
    IEnumerable<(string Path, ImmutableArray<CopyItem> CopyItems)> ItemsByProject,
    bool IsComplete,
    IReadOnlyList<string>? TargetsWithoutReferenceAssemblies);

/// <summary>
/// Models the set of copy items a project produces, along with some details about the project.
/// </summary>
/// <param name="ProjectFullPath">The full path to the project file (e.g. the <c>.csproj</c> file).</param>
/// <param name="TargetPath">The full path to the target file (e.g. a <c>.dll</c> file), which should be unique to the configuration.</param>
/// <param name="ProduceReferenceAssembly">Whether this project produces a reference assembly or not, determined by the <c>ProduceReferenceAssembly</c> MSBuild property.</param>
/// <param name="CopyItems">The set of items the project provider to the output directory of itself and other projects.</param>
/// <param name="ReferencedProjectTargetPaths">The target paths resolved from this project's references to other projects.</param>
internal record struct ProjectCopyData(
    string? ProjectFullPath,
    string TargetPath,
    bool ProduceReferenceAssembly,
    ImmutableArray<CopyItem> CopyItems,
    ImmutableArray<string> ReferencedProjectTargetPaths)
{
    public readonly bool IsDefault => CopyItems.IsDefault;
}
