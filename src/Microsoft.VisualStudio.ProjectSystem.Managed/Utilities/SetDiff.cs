// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Microsoft.VisualStudio.ProjectSystem;

internal sealed class SetDiff<T> where T : notnull
{
    private const byte FlagBefore = 0;
    private const byte FlagAfter = 1;

    private readonly Dictionary<T, byte> _dic;

    public Part Removed => new(_dic, FlagBefore);

    public Part Added => new(_dic, FlagAfter);

    public bool HasChange => _dic.Count is not 0;

    public SetDiff(IEnumerable<T> before, IEnumerable<T> after, IEqualityComparer<T>? equalityComparer = null)
    {
        Requires.NotNull(before);
        Requires.NotNull(after);

        equalityComparer ??= EqualityComparer<T>.Default;

        Dictionary<T, byte> dic = new(equalityComparer);

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

    public readonly struct Part(Dictionary<T, byte> dic, byte flag) : IEnumerable<T>
    {
        public PartEnumerator GetEnumerator() => new(dic, flag);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public struct PartEnumerator(Dictionary<T, byte> dic, byte flag) : IEnumerator<T>
        {
            // IMPORTANT cannot be readonly
            private Dictionary<T, byte>.Enumerator _enumerator = dic.GetEnumerator();

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    if (_enumerator.Current.Value == flag)
                    {
                        Current = _enumerator.Current.Key;
                        return true;
                    }
                }

                return false;
            }

            public T Current { get; private set; } = default!;

            readonly object IEnumerator.Current => Current!;

            readonly void IEnumerator.Reset() => throw new NotSupportedException();

            readonly void IDisposable.Dispose() { }
        }
    }
}
