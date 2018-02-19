using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xunit
{
    public static class AssertEx
    {
        public static void CollectionLength<T>(IEnumerable<T> collection, int length)
        {
            var lengthArray = new Action<T>[length];
            for (int i = 0; i < length; i++)
            {
                lengthArray[i] = delegate { };
            }
            Assert.Collection(collection, lengthArray);
        }

        public static void CollectionLength(IEnumerable collection, int length)
        {
            CollectionLength(collection.Cast<object>(), length);
        }
    }
}
