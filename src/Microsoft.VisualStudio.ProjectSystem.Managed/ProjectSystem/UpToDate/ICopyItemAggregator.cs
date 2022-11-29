// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

/// <summary>
/// A global component that tracks the items each project contributes to the output directories of those projects that reference it, both directly and transitively.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface ICopyItemAggregator
{
    void SetProjectData(ProjectCopyData projectCopyData);

    (IEnumerable<CopyItem> Items, bool IsComplete) TryGatherCopyItemsForProject(string targetPath, BuildUpToDateCheck.Log logger);
}

internal record struct ProjectCopyData(
    string? ProjectFullPath,
    string TargetPath,
    ImmutableArray<CopyItem> CopyItems,
    ImmutableArray<string> ReferencedProjectTargetPaths);
