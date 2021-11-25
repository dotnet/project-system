// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Xunit.Sdk;

namespace Xunit
{
    internal static class AssertEx
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

        public static void SequenceSame<T>(IEnumerable<T> expected, IEnumerable<T> actual) where T : class
        {
            using IEnumerator<T> expectedEnumerator = expected.GetEnumerator();
            using IEnumerator<T> actualEnumerator = actual.GetEnumerator();

            while (true)
            {
                bool nextExpected = expectedEnumerator.MoveNext();
                bool nextActual = actualEnumerator.MoveNext();

                if (nextExpected && nextActual)
                {
                    Assert.Same(expectedEnumerator.Current, actualEnumerator.Current);
                }
                else if (!nextExpected && !nextActual)
                {
                    return;
                }
                else
                {
                    throw new XunitException("Sequences have different lengths");
                }
            }
        }
    }
}
