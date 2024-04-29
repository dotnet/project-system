// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

/// <summary>
/// A global component that tracks the items each project contributes to the output directories of those projects that reference it, both directly and transitively.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface ICopyItemAggregator
{
    /// <summary>
    /// Integrates data from a project, for use within <see cref="TryGatherCopyItemsForProject"/>.
    /// </summary>
    /// <param name="projectCopyData">All necessary data about the project that's providing the data.</param>
    void SetProjectData(ProjectCopyData projectCopyData);

    /// <summary>
    /// Walks the project reference graph in order to obtain all reachable copy items
    /// from the project identified by <paramref name="targetPath"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We use the <c>TargetPath</c> property to identify projects, as that path takes
    /// other dimensions (such as target framework, platform, etc.) into account when projects
    /// have multiple configurations.
    /// </para>
    /// <para>
    /// When multiple copy items want to write to the same relative output path, the set of duplicate
    /// items is written to <see cref="CopyItemsResult.DuplicateCopyItemRelativeTargetPaths"/>.
    /// Build Acceleration disables itself when items exist in this collection, as it does not
    /// attempt to reproduce the full behaviour of MSBuild for this scenario.
    /// </para>
    /// <para>
    /// If we find a reference to a project that did not call <see cref="SetProjectData"/> then
    /// the returned <see cref="CopyItemsResult.IsComplete"/> will be <see langword="false"/>.
    /// Build Acceleration disables itself when copy items from a reachable project is unavailable.
    /// </para>
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
/// <param name="IsComplete">Indicates whether we have items from all reachable projects.</param>
/// <param name="ItemsByProject">
///     A sequence of items by project, that are reachable from the current project. The path is that
///     of the project file, such as <c>c:\repos\MyProject\MyProject.csproj</c>.
/// </param>
/// <param name="DuplicateCopyItemRelativeTargetPaths">
///     A list of relative target paths for which more than one project produces an item, or <see langword="null"/> if
///     no duplicates exists.
/// </param>
/// <param name="TargetsWithoutReferenceAssemblies">
///     A list of target paths for projects that do not produce reference assemblies, or <see langword="null"/> if
///     all reachable projects do in fact produce reference assemblies.
/// </param>
internal record struct CopyItemsResult(
    bool IsComplete,
    IEnumerable<(string Path, ImmutableArray<CopyItem> CopyItems)> ItemsByProject,
    IReadOnlyList<string>? DuplicateCopyItemRelativeTargetPaths,
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
