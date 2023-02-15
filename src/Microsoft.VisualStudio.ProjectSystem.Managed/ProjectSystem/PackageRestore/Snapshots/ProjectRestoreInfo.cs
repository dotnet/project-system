// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     A complete set of restore data for a project.
    /// </summary>
    internal class ProjectRestoreInfo
    {
        // If additional fields/properties are added to this class, please update RestoreHasher

        public ProjectRestoreInfo(string msbuildProjectExtensionsPath, string projectAssetsFilePath, string originalTargetFrameworks, ImmutableArray<TargetFrameworkInfo> targetFrameworks, ImmutableArray<ReferenceItem> toolReferences)
        {
            MSBuildProjectExtensionsPath = msbuildProjectExtensionsPath;
            ProjectAssetsFilePath = projectAssetsFilePath;
            OriginalTargetFrameworks = originalTargetFrameworks;
            TargetFrameworks = targetFrameworks;
            ToolReferences = toolReferences;
        }

        public string MSBuildProjectExtensionsPath { get; }

        public string ProjectAssetsFilePath { get; }

        public string OriginalTargetFrameworks { get; }

        public ImmutableArray<TargetFrameworkInfo> TargetFrameworks { get; }

        public ImmutableArray<ReferenceItem> ToolReferences { get; }
    }
}
