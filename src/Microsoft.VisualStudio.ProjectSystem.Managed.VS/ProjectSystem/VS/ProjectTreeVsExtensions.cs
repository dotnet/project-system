// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class ProjectTreeVsExtensions
    {
        /// <summary>
        ///     Returns the specified <see cref="HierarchyId"/> for the specified node.
        /// </summary>
        /// <remarks>
        ///     This method should only be called on and the result used on the UI thread.
        /// </remarks>
        public static HierarchyId GetHierarchyId(this IProjectTree node)
        {
            return new HierarchyId(node.IsRoot() ? VSConstants.VSITEMID_ROOT : unchecked((uint)node.Identity));
        }
    }
}
