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

        /// <summary>
        /// Detects the target configuration dimension associated with a given hierarchy item in the dependencies tree, if
        /// nested within a target group node. For projects that do not multi-target, this will always return <see langword="false"/>.
        /// This method searches ancestors until a target is found, or the project root is found.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <param name="target">The detected target, if found.</param>
        /// <returns><see langword="true"/> if the target was found, otherwise <see langword="false"/>.</returns>
        public static bool TryFindTarget(this IVsHierarchyItem item, [NotNullWhen(returnValue: true)] out string? target)
        {
            s_targetFlagsRegex ??= new Regex(@"^(?=.*\b" + nameof(DependencyTreeFlags.TargetNode) + @"\b)(?=.*\$TFM:(?<target>[^ ]+)\b).*$", RegexOptions.Compiled);

            for (IVsHierarchyItem? parent = item; parent is not null; parent = parent.Parent)
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
