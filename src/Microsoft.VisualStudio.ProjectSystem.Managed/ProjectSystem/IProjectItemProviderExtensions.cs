// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemProviderExtensions
    {
        public static async Task<IProjectItem?> GetItemAsync(this IProjectItemProvider provider, string itemType, Func<IProjectItem, Task<bool>> condition)
        {
            foreach (IProjectItem item in await provider.GetItemsAsync(itemType))
            {
                if (await condition(item))
                {
                    return item;
                }
            }

            return null;
        }

        public static async Task<IProjectItem?> GetItemAsync(this IProjectItemProvider provider, string itemType, Predicate<IProjectItem> condition)
        {
            foreach (IProjectItem item in await provider.GetItemsAsync(itemType))
            {
                if (condition(item))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
