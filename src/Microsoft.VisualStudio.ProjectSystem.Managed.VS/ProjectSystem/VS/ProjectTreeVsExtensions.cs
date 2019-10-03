// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
