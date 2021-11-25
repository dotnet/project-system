// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Models a project source item of a type that is known to be an up-to-date check input.
    /// </summary>
    internal readonly struct UpToDateCheckInputItem
    {
        public static readonly IEqualityComparer<UpToDateCheckInputItem> PathComparer = new UpToDateCheckInputItemPathComparer();

        /// <summary>
        /// Gets the relative path to the item.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the optional path to which this item is copied.
        /// </summary>
        /// <remarks>
        /// Only applicable when <see cref="CopyType"/> indicates the item is copied.
        /// </remarks>
        public string? TargetPath { get; }

        /// <summary>
        /// Gets a value indicating if and when the item is copied during build.
        /// </summary>
        public BuildUpToDateCheck.CopyType CopyType { get; }

        public UpToDateCheckInputItem(string path, string? targetPath, BuildUpToDateCheck.CopyType copyType)
        {
            Path = path;
            TargetPath = targetPath;
            CopyType = copyType;
        }

        public UpToDateCheckInputItem(string path, IImmutableDictionary<string, string> metadata)
        {
            Path = path;
            TargetPath = GetTargetPath();
            CopyType = GetCopyType();

            string? GetTargetPath()
            {
                // "Link" is an optional path and file name under which the item should be copied.
                // It allows a source file to be moved to a different relative path, or to be renamed.
                //
                // From the perspective of the FUTD check, it is only relevant on CopyToOutputDirectory items.
                //
                // Two properties can provide this feature: "Link" and "TargetPath".
                //
                // If specified, "TargetPath" metadata controls the path of the target file, relative to the output
                // folder.
                //
                // "Link" controls the location under the project in Solution Explorer where the item appears.
                // If "TargetPath" is not specified, then "Link" can also serve the role of "TargetPath".
                //
                // If both are specified, we only use "TargetPath". The use case for specifying both is wanting
                // to control the location of the item in Solution Explorer, as well as in the output directory.
                // The former is not relevant to us here.

                if (metadata.TryGetValue(None.TargetPathProperty, out string? targetPath) && !string.IsNullOrWhiteSpace(targetPath))
                {
                    return targetPath;
                }

                if (metadata.TryGetValue(None.LinkProperty, out string link) && !string.IsNullOrWhiteSpace(link))
                {
                    return link;
                }

                return null;
            }

            BuildUpToDateCheck.CopyType GetCopyType()
            {
                if (metadata.TryGetValue(Compile.CopyToOutputDirectoryProperty, out string value))
                {
                    if (string.Equals(value, Compile.CopyToOutputDirectoryValues.Always, StringComparisons.PropertyLiteralValues))
                    {
                        return BuildUpToDateCheck.CopyType.CopyAlways;
                    }

                    if (string.Equals(value, Compile.CopyToOutputDirectoryValues.PreserveNewest, StringComparisons.PropertyLiteralValues))
                    {
                        return BuildUpToDateCheck.CopyType.CopyIfNewer;
                    }
                }

                return BuildUpToDateCheck.CopyType.CopyNever;
            }
        }

        private sealed class UpToDateCheckInputItemPathComparer : IEqualityComparer<UpToDateCheckInputItem>
        {
            public UpToDateCheckInputItemPathComparer() { }

            public bool Equals(UpToDateCheckInputItem x, UpToDateCheckInputItem y) => StringComparers.Paths.Equals(x.Path, y.Path);

            public int GetHashCode(UpToDateCheckInputItem obj) => StringComparers.Paths.GetHashCode(obj.Path);
        }
    }
}
