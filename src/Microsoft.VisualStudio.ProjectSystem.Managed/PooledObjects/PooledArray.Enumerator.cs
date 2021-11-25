// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    internal sealed partial class PooledArray<T>
    {
        /// <summary>
        /// struct enumerator used in foreach.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly PooledArray<T> _builder;
            private int _index;

            public Enumerator(PooledArray<T> builder)
            {
                _builder = builder;
                _index = -1;
            }

            public T Current => _builder[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _builder.Count;
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current => Current!;

            public void Reset() => _index = -1;
        }
    }
}
