// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemProviderExtensions
    {
        public static async Task<IProjectItem?> GetItemAsync(this IProjectItemProvider provider, string itemType, Func<IProjectItem, Task<bool>> predicate)
        {
            foreach (IProjectItem item in await provider.GetItemsAsync(itemType))
            {
                if (await predicate(item))
                {
                    return item;
                }
            }

            return null;
        }

        public static async Task<IProjectItem?> GetItemAsync(this IProjectItemProvider provider, string itemType, Func<IProjectItem, bool> predicate)
        {
            foreach (IProjectItem item in await provider.GetItemsAsync(itemType))
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
