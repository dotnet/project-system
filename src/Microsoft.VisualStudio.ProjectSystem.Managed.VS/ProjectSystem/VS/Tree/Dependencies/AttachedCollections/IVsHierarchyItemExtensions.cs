// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;

using Flags = Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.DependencyTreeFlags;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Extension methods for <see cref="IVsHierarchyItem"/> that support attached collections.
    /// </summary>
    internal static class IVsHierarchyItemExtensions
    {
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
            // Check this hierarchy item, then walk upwards through its ancestry.
            for (; item is not null; item = item.Parent)
            {
                if (item.TryGetFlags(out ProjectTreeFlags flags) && flags.Contains(Flags.TargetNode))
                {
                    // Found an ancestor target node.
                    const string prefix = "$TFM:";
                    string? flag = flags.FirstOrDefault(f => f.StartsWith(prefix));

                    if (flag is not null)
                    {
                        target = flag.Substring(prefix.Length);
                        return true;
                    }

                    // Target node didn't have a TFM flag for some reason. This is unexpected, but there's not much
                    // we can do about it. Don't check any more ancestors, as there should only be one relevant target node.
                    break;
                }
            }

            target = null;
            return false;
        }

        public static bool TryGetFlags(this IVsHierarchyItem item, out ProjectTreeFlags flags)
        {
            // Browse objects are created lazily, so access the one on the project root in order to obtain the unconfigured project.
            IVsHierarchyItem root = item;
            while (!root.HierarchyIdentity.IsRoot)
            {
                root = root.Parent;
            }

            if (root.HierarchyIdentity.NestedHierarchy is IVsBrowseObjectContext context)
            {
                IProjectTreeServiceState? tree = context.UnconfiguredProject.Services.ProjectTreeService?.CurrentTree;

                if (tree is not null)
                {
                    if (tree.Tree.TryFind((IntPtr)item.HierarchyIdentity.ItemID, out IProjectTree? subtree))
                    {
                        flags = subtree.Flags;
                        return true;
                    }
                }
            }

            flags = default;
            return false;
        }
    }
}
