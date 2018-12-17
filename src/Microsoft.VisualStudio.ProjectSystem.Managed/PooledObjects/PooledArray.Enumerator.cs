// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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

#pragma warning disable CS8603 // Workaround https://github.com/dotnet/roslyn/issues/31867
            object System.Collections.IEnumerator.Current => Current;
#pragma warning restore CS8603

            public void Reset() => _index = -1;
        }
    }
}
