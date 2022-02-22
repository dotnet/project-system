// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Models an item that is copied to a project's output directory when that project is built.
    /// </summary>
    internal readonly struct CopyItem
    {
        /// <summary>
        /// Gets the path to the item, relative to the project.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets path to which this item is copied, relative to the output directory.
        /// </summary>
        public string TargetPath { get; }

        /// <summary>
        /// Gets a value indicating when the item is copied during build.
        /// </summary>
        public BuildUpToDateCheck.CopyType CopyType { get; }

        public CopyItem(string path, string targetPath, BuildUpToDateCheck.CopyType copyType)
        {
            Requires.NotNull(targetPath, nameof(targetPath));

            Path = path;
            TargetPath = targetPath;
            CopyType = copyType;
        }

        public void Deconstruct(out string path, out string targetPath, out BuildUpToDateCheck.CopyType copyType)
        {
            path = Path;
            targetPath = TargetPath;
            copyType = CopyType;
        }
    }
}
