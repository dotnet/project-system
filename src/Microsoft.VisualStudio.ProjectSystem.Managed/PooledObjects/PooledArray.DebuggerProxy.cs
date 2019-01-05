// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    internal sealed partial class PooledArray<T>
    {
        private sealed class DebuggerProxy
        {
            private readonly PooledArray<T> _builder;

            public DebuggerProxy(PooledArray<T> builder)
            {
                _builder = builder;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] A
            {
                get
                {
                    var result = new T[_builder.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = _builder[i];
                    }

                    return result;
                }
            }
        }
    }
}
