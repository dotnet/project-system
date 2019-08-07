// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace Xunit
{
    public static class AssertEx
    {
        public static void CollectionLength<T>(IEnumerable<T> collection, int length)
        {
            var lengthArray = new Action<T>[length];
            for (int i = 0; i < length; i++)
            {
                lengthArray[i] = delegate
                { };
            }
            Assert.Collection(collection, lengthArray);
        }

        public static void CollectionLength(IEnumerable collection, int length)
        {
            CollectionLength(collection.Cast<object>(), length);
        }
    }
}
