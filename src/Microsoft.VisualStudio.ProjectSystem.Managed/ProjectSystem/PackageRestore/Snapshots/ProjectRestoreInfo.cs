// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
/// A complete set of restore data for a project.
/// </summary>
internal sealed class ProjectRestoreInfo(
    string msbuildProjectExtensionsPath,
    string projectAssetsFilePath,
    string originalTargetFrameworks,
    ImmutableArray<TargetFrameworkInfo> targetFrameworks,
    ImmutableArray<ReferenceItem> toolReferences)
    : IRestoreState<ProjectRestoreInfo>
{
    // IMPORTANT: If additional state is added, update AddToHash and DescribeChanges below.

    public string MSBuildProjectExtensionsPath { get; } = msbuildProjectExtensionsPath;

    public string ProjectAssetsFilePath { get; } = projectAssetsFilePath;

    public string OriginalTargetFrameworks { get; } = originalTargetFrameworks;

    public ImmutableArray<TargetFrameworkInfo> TargetFrameworks { get; } = targetFrameworks;

    public ImmutableArray<ReferenceItem> ToolReferences { get; } = toolReferences;

    public void AddToHash(IncrementalHasher hasher)
    {
        hasher.AppendProperty(nameof(ProjectAssetsFilePath),        ProjectAssetsFilePath);
        hasher.AppendProperty(nameof(MSBuildProjectExtensionsPath), MSBuildProjectExtensionsPath);
        hasher.AppendProperty(nameof(OriginalTargetFrameworks),     OriginalTargetFrameworks);

        hasher.AppendArray(TargetFrameworks);
        hasher.AppendArray(ToolReferences);
    }

    public void DescribeChanges(RestoreStateComparisonBuilder builder, ProjectRestoreInfo after)
    {
        builder.CompareString(MSBuildProjectExtensionsPath, after.MSBuildProjectExtensionsPath, nameof(MSBuildProjectExtensionsPath));
        builder.CompareString(ProjectAssetsFilePath, after.ProjectAssetsFilePath, nameof(ProjectAssetsFilePath));
        builder.CompareString(OriginalTargetFrameworks, after.OriginalTargetFrameworks, nameof(OriginalTargetFrameworks));

        builder.CompareArray(TargetFrameworks, after.TargetFrameworks, nameof(TargetFrameworks));
        builder.CompareArray(ToolReferences, after.ToolReferences, nameof(ToolReferences));
    }
}
