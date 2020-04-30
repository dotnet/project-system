// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.ProjectModel;

namespace Microsoft.VisualStudio.NuGet.Models
{
    /// <summary>
    /// Data about a a content file within a package in a given target, from <c>project.assets.json</c>. Immutable.
    /// </summary>
    internal sealed class AssetsFileTargetLibraryContentFile
    {
        public AssetsFileTargetLibraryContentFile(LockFileContentFile file)
        {
            Requires.NotNull(file, nameof(file));
            
            BuildAction = file.BuildAction.Value;
            CodeLanguage = file.CodeLanguage;
            CopyToOutput = file.CopyToOutput;
            OutputPath = file.OutputPath;
            Path = file.Path ?? ""; // Path should always be present so don't require consumers to null check. Not worth throwing if null though.
            PPOutputPath = file.PPOutputPath;
        }

        public string? BuildAction { get; }
        public string? CodeLanguage { get; }
        public bool CopyToOutput { get; }
        public string? OutputPath { get; }
        public string Path { get; }
        public string? PPOutputPath { get; }
    }
}
