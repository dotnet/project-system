// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
