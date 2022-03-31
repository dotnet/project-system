// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Models a project source item of a type that is known to be an up-to-date check input.
    /// </summary>
    internal readonly struct UpToDateCheckInputItem
    {
        public static readonly IEqualityComparer<UpToDateCheckInputItem> PathComparer = new UpToDateCheckInputItemPathComparer();

        /// <summary>
        /// The set of item types which are eligible to be copied to the project's output directory
        /// when <c>CopyToOutputDirectory</c> metadata is present.
        /// </summary>
        /// <remarks>
        /// Mirrors the items specified in MSBuild's <c>_GetCopyToOutputDirectoryItemsFromThisProject</c>
        /// target.
        /// </remarks>
        private static readonly ImmutableHashSet<string> s_copyToOutputDirectoryItemTypes
            = ImmutableHashSet<string>.Empty.WithComparer(StringComparers.ItemTypes)
                .Add(None.SchemaName)
                .Add(Content.SchemaName)
                .Add(Compile.SchemaName)
                .Add("EmbeddedResource");

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

        public UpToDateCheckInputItem(string path, string itemType, IImmutableDictionary<string, string> metadata)
        {
            Path = path;

            // We only track copies to a target path for certain item types.
            // For other item types, set a null target path, and a copy type of "never".
            bool isCopyToOutputDirectoryType = s_copyToOutputDirectoryItemTypes.Contains(itemType);

            TargetPath = isCopyToOutputDirectoryType ? GetTargetPath() : null;
            CopyType = isCopyToOutputDirectoryType ? GetCopyType() : BuildUpToDateCheck.CopyType.Never;

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
                if (metadata.TryGetValue(None.CopyToOutputDirectoryProperty, out string value))
                {
                    if (string.Equals(value, None.CopyToOutputDirectoryValues.Always, StringComparisons.PropertyLiteralValues))
                    {
                        return BuildUpToDateCheck.CopyType.Always;
                    }

                    if (string.Equals(value, None.CopyToOutputDirectoryValues.PreserveNewest, StringComparisons.PropertyLiteralValues))
                    {
                        return BuildUpToDateCheck.CopyType.PreserveNewest;
                    }
                }

                return BuildUpToDateCheck.CopyType.Never;
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
