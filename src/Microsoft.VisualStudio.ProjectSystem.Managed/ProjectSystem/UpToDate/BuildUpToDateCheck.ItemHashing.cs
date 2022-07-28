// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        public static int ComputeItemHash(ImmutableDictionary<string, ImmutableArray<UpToDateCheckInputItem>> itemsByItemType)
        {
            int hash = 0;

            // Use XOR so the order of items is not important. We are using string hash codes which are
            // quite well distributed. This approach might not work as well for other types, such as integers.
            //
            // This approach also assumes each path is only included once in the data structure. If a path
            // were to exist twice, its hash would be XORed with itself, which produces zero net change.

            foreach ((string itemType, ImmutableArray<UpToDateCheckInputItem> items) in itemsByItemType)
            {
                int itemHash = 0;

                foreach (UpToDateCheckInputItem item in items)
                {
                    itemHash ^= GetStableHashCode(item.Path);
                }

                // Multiply by the item type hash, so that if an item changes type the hash will change.
                // The rest of the system does not really need this though, as it is assumed the only way
                // an item can change type is if a project file changes, which would be detected via
                // file timestamp changes.
                hash ^= itemHash * GetStableHashCode(itemType);
            }

            return hash;
        }

        /// <summary>
        /// Please, do not make changes to this hash algorithm.
        /// Current hash value is persisted in a file in the .vs folder, 
        /// changing this algorithm may regress performance and break compatibility.
        /// 
        /// The original code was taken from string.GetHashCode() with some minor changes
        /// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/string.cs
        /// </summary>
        internal static int GetStableHashCode(string str)
        {
            unsafe
            {
                fixed (char* src = str)
                {
#if WIN32
                int hash1 = (5381<<16) + 5381;
#else
                    int hash1 = 5381;
#endif
                    int hash2 = hash1;

#if WIN32
                // 32 bit machines.
                int* pint = (int *)src;
                int len = this.Length;
                while (len > 2)
                {
                    hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                    pint += 2;
                    len  -= 4;
                }

                if (len > 0)
                {
                    hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                }
#else
                    int c;
                    char* s = src;
                    while ((c = s[0]) != 0)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }
#endif
                    return hash1 + (hash2 * 1566083941);
                }
            }
        }
    }
}
