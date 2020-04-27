// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Specifies the order of attached items in the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Used in conjunction with <see cref="IPrioritizedComparable"/>.
    /// </remarks>
    internal static class AttachedItemPriority
    {
        // Not all of these can be siblings.

        public const int Diagnostic               = 100;
        public const int Package                  = 200;
        public const int Project                  = 300;
        public const int CompileTimeAssemblyGroup = 400;
        public const int FrameworkAssemblyGroup   = 500;
        public const int ContentFilesGroup        = 600;
    }
}
