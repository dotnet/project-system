// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Extension methods for <see cref="IVsHierarchyItem"/> that support attached collections.
    /// </summary>
    internal static class IVsHierarchyItemExtensions
    {
        private static Regex? s_targetFlagsRegex;
        private static Regex? s_packageFlagsRegex;
        private static Regex? s_projectFlagsRegex;

        /// <summary>
        /// Detects the target configuration dimension associated with a given hierarchy item in the dependencies tree, if
        /// nested within a target group node. For projects that do not multi-target, this will always return <see langword="false"/>.
        /// This method searches ancestors until a target is found, or the project root is found.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <param name="target">The detected target, if found.</param>
        /// <returns><see langword="true"/> if the target was found, otherwise <see langword="false"/>.</returns>
        public static bool TryFindTarget(
            this IVsHierarchyItem item,
            [NotNullWhen(returnValue: true)] out string? target)
        {
            s_targetFlagsRegex ??= new Regex(@"^(?=.*\b" + nameof(DependencyTreeFlags.TargetNode) + @"\b)(?=.*\$TFM:(?<target>[^ ]+)\b).*$", RegexOptions.Compiled);

            for (IVsHierarchyItem? parent = item; parent != null; parent = parent.Parent)
            {
                if (parent.TryGetFlagsString(out string? flagsString))
                {
                    Match match = s_targetFlagsRegex.Match(flagsString);
                    if (match.Success)
                    {
                        target = match.Groups["target"].Value;
                        return true;
                    }
                }
            }

            target = null;
            return false;
        }

        /// <summary>
        /// Detects the package ID and version associated with a given hierarchy item in the dependencies tree, if
        /// that node represents a package reference.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <param name="packageId">The detected package ID, if found.</param>
        /// <param name="packageVersion">The detected package version, if found.</param>
        /// <returns><see langword="true"/> if package ID and version were found, otherwise <see langword="false"/>.</returns>
        public static bool TryGetPackageDetails(
            this IVsHierarchyItem item,
            [NotNullWhen(returnValue: true)] out string? packageId,
            [NotNullWhen(returnValue: true)] out string? packageVersion)
        {
            s_packageFlagsRegex ??= new Regex(@"^(?=.*\b" + nameof(DependencyTreeFlags.PackageDependency) + @"\b)(?=.*\$ID:(?<id>[^ ]+)\b)(?=.*\$VER:(?<version>[^ ]+)\b).*$", RegexOptions.Compiled);

            if (item.TryGetFlagsString(out string? flagsString))
            {
                return TryGetPackageDetails(flagsString, out packageId, out packageVersion);
            }

            packageId = null;
            packageVersion = null;
            return false;
        }

        /// <summary>
        /// Detects the package ID and version within a dependencies tree item's flags string, if
        /// that node represents a package reference.
        /// </summary>
        /// <param name="flagsString">The string of flags to inspect.</param>
        /// <param name="packageId">The detected package ID, if found.</param>
        /// <param name="packageVersion">The detected package version, if found.</param>
        /// <returns><see langword="true"/> if package ID and version were found, otherwise <see langword="false"/>.</returns>
        public static bool TryGetPackageDetails(
            string flagsString,
            [NotNullWhen(returnValue: true)] out string? packageId,
            [NotNullWhen(returnValue: true)] out string? packageVersion)
        {
            s_packageFlagsRegex ??= new Regex(@"^(?=.*\b" + nameof(DependencyTreeFlags.PackageDependency) + @"\b)(?=.*\$ID:(?<id>[^ ]+)\b)(?=.*\$VER:(?<version>[^ ]+)\b).*$", RegexOptions.Compiled);

            Match match = s_packageFlagsRegex.Match(flagsString);

            if (match.Success)
            {
                packageId = match.Groups["id"].Value;
                packageVersion = match.Groups["version"].Value;
                return true;
            }

            packageId = null;
            packageVersion = null;
            return false;
        }

        /// <summary>
        /// Detects the project associated with a given hierarchy item in the dependencies tree, if
        /// that node represents a project reference.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <param name="projectId">The detected project ID, if found.</param>
        /// <returns><see langword="true"/> if project ID was found, otherwise <see langword="false"/>.</returns>
        public static bool TryGetProjectDetails(
            this IVsHierarchyItem item,
            [NotNullWhen(returnValue: true)] out string? projectId)
        {
            if (item.TryGetFlagsString(out string? flagsString))
            {
                if (TryGetProjectDetails(flagsString, out projectId))
                {
                    return true;
                }
            }

            projectId = null;
            return false;
        }

        /// <summary>
        /// Detects the project ID within a dependencies tree item's flags string, if
        /// that node represents a project reference.
        /// </summary>
        /// <param name="flagsString">The string of flags to inspect.</param>
        /// <param name="projectId">The detected project ID, if found.</param>
        /// <returns><see langword="true"/> if project ID was found, otherwise <see langword="false"/>.</returns>
        public static bool TryGetProjectDetails(
            string flagsString,
            [NotNullWhen(returnValue: true)] out string? projectId)
        {
            s_projectFlagsRegex ??= new Regex(@"^(?=.*\b" + nameof(DependencyTreeFlags.ProjectDependency) + @"\b)(?=.*\$ID:(?<id>[^ ]+)\b).*$", RegexOptions.Compiled);

            Match match = s_projectFlagsRegex.Match(flagsString);
            if (match.Success)
            {
                projectId = match.Groups["id"].Value;
                return true;
            }

            projectId = null;
            return false;
        }

        public static bool TryGetFlagsString(this IVsHierarchyItem item, [NotNullWhen(returnValue: true)] out string? flagsString)
        {
            IVsHierarchyItemIdentity identity = item.HierarchyIdentity;

            if (identity.NestedHierarchy.GetProperty(identity.NestedItemID, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out object? value) == HResult.OK)
            {
                flagsString = (string)value;
                return true;
            }

            flagsString = null;
            return false;
        }
    }
}
