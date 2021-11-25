// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Given an <see cref="IEnumerable{T}"/>, returns a new <see cref="IEnumerable{T}"/> that yields tuples with
        /// the items of the original plus their index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<(int index, T item)> WithIndices<T>(this IEnumerable<T> enumerable)
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                yield return (index++, item);
            }
        }
    }
}
