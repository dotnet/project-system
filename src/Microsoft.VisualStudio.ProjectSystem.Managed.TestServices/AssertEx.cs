// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xunit.Sdk;

#nullable disable

namespace Xunit
{
    public static class AssertEx
    {
        public static void CollectionLength<T>(IEnumerable<T> collection, int expectedCount)
        {
            int actualCount = collection.Count();

            if (actualCount != expectedCount)
            {
                throw new CollectionException(collection, expectedCount, actualCount);
            }
        }

        public static void CollectionLength(IEnumerable collection, int expectedCount)
        {
            CollectionLength(collection.Cast<object>(), expectedCount);
        }
    }
}
