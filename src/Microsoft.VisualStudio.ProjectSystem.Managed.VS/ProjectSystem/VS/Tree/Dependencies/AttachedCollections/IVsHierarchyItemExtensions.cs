// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Extension methods for <see cref="IVsHierarchyItem"/> that support attached collections.
    /// </summary>
    internal static class IVsHierarchyItemExtensions
    {
        public static bool TryGetFlagsString(this IVsHierarchyItem item, [NotNullWhen(returnValue: true)] out string? flagsString)
        {
            IVsHierarchyItemIdentity identity = item.HierarchyIdentity;

            if (identity.NestedHierarchy.GetProperty(identity.NestedItemID, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out object? value) == HResult.OK)
            {
                flagsString = (string)value;
                return true;
            }

            flagsString = default;
            return false;
        }
    }
}
