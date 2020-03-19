// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Specifies the order of attached items in the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Used in conjunction with <see cref="IPrioritizedComparable"/>.
    /// </remarks>
    internal static class AttachedItemPriority
    {
        public const int Diagnostic = 1;
    }
}
