// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class SetDiff<T> where T : notnull
    {
        private const byte FlagBefore = 0;
        private const byte FlagAfter = 1;

        private readonly Dictionary<T, byte> _dic;

        public Part Removed => new(_dic, FlagBefore);

        public Part Added => new(_dic, FlagAfter);

        public SetDiff(IEnumerable<T> before, IEnumerable<T> after, IEqualityComparer<T>? equalityComparer = null)
        {
            Requires.NotNull(before, nameof(before));
            Requires.NotNull(after, nameof(after));

            equalityComparer ??= EqualityComparer<T>.Default;

            var dic = new Dictionary<T, byte>(equalityComparer);

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

            public PartEnumerator GetEnumerator() => new(_dic, _flag);

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
