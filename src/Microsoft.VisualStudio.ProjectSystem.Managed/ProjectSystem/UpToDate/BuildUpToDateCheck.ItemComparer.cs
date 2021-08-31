// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        internal sealed class ItemComparer : IEqualityComparer<(string path, string? targetPath, CopyType copyType)>
        {
            public static readonly ItemComparer Instance = new();

            private ItemComparer()
            {
            }

            public bool Equals(
                (string path, string? targetPath, CopyType copyType) x,
                (string path, string? targetPath, CopyType copyType) y)
            {
                return StringComparers.Paths.Equals(x.path, y.path);
            }

            public int GetHashCode(
                (string path, string? targetPath, CopyType copyType) obj)
            {
                return StringComparers.Paths.GetHashCode(obj.path);
            }
        }
    }
}
