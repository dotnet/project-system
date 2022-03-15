// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft
{
    public static class LazyExtensions
    {
        public static Lazy<T> AsLazy<T>(this T instance)
        {
            return new Lazy<T>(() => instance);
        }
    }
}
