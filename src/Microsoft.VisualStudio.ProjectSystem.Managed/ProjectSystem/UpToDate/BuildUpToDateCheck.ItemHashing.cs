// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        public static int ComputeItemHash(ImmutableDictionary<string, ImmutableArray<(string Path, string? TargetPath, CopyType CopyType)>> itemsByItemType)
        {
            int hash = 0;

            // Use XOR so the order of items is not important. We are using string hash codes which are
            // quite well distributed. This approach might not work as well for other types, such as integers.
            //
            // This approach also assumes each path is only included once in the data structure. If a path
            // were to exist twice, its hash would be XORed with itself, which produces zero net change.

            foreach ((string itemType, ImmutableArray<(string Path, string? TargetPath, CopyType CopyType)> items) in itemsByItemType)
            {
                int itemHash = 0;

                foreach ((string path, _, _) in items)
                {
                    itemHash ^= path.GetHashCode();
                }

                // Multiply by the item type hash, so that if an item changes type the hash will change.
                // The rest of the system does not really need this though, as it is assumed the only way
                // an item can change type is if a project file changes, which would be detected via
                // file timestamp changes.
                hash ^= itemHash * itemType.GetHashCode();
            }

            return hash;
        }
    }
}
