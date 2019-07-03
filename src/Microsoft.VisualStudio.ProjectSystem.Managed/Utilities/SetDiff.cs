// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class SetDiff<T>
    {
        private const byte FlagBefore = 0;
        private const byte FlagAfter = 1;

        private readonly Dictionary<T, byte> _dic;

        public Part Removed => new Part(_dic, FlagBefore);

        public Part Added => new Part(_dic, FlagAfter);

        public SetDiff(IEnumerable<T> before, IEnumerable<T> after)
        {
            Requires.NotNull(before, nameof(before));
            Requires.NotNull(after, nameof(after));

            var dic = new Dictionary<T, byte>();

            foreach (T item in before)
            {
                dic[item] = FlagBefore;
            }

            foreach (T item in after)
            {
                if (!dic.Remove(item))
                {
                    dic[item] = FlagAfter;
                }
            }

            _dic = dic;
        }

        public readonly struct Part : IEnumerable<T>
        {
            private readonly Dictionary<T, byte> _dic;
            private readonly byte _flag;

            public Part(Dictionary<T, byte> dic, byte flag)
            {
                _dic = dic;
                _flag = flag;
            }

            public PartEnumerator GetEnumerator() => new PartEnumerator(_dic, _flag);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

            public struct PartEnumerator : IEnumerator<T>
            {
                private readonly byte _flag;

                // IMPORTANT cannot be readonly
                private Dictionary<T, byte>.Enumerator _enumerator;

                public PartEnumerator(Dictionary<T, byte> dic, byte flag)
                {
                    _flag = flag;
                    _enumerator = dic.GetEnumerator();
                    Current = default!;
                }

                public bool MoveNext()
                {
                    while (_enumerator.MoveNext())
                    {
                        if (_enumerator.Current.Value == _flag)
                        {
                            Current = _enumerator.Current.Key;
                            return true;
                        }
                    }

                    return false;
                }

                public T Current { get; private set; }

                object IEnumerator.Current => Current!;

                void IEnumerator.Reset() => throw new NotSupportedException();

                void IDisposable.Dispose() { }
            }
        }
    }
}
