﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Microsoft.VisualStudio.Text;

/// <summary>
///     Splits a string by a delimiter, producing substrings lazily during enumeration.
///     Skips empty items, behaving equivalently to <see cref="string.Split(char[])"/> with
///     <see cref="StringSplitOptions.RemoveEmptyEntries"/>.
/// </summary>
/// <remarks>
///     Unlike <see cref="string.Split(char[])"/> and overloads, <see cref="LazyStringSplit"/>
///     does not allocate an array for the return, and allocates strings on demand during
///     enumeration. A custom enumerator type is used so that the only allocations made are
///     the substrings themselves. We also avoid the large internal arrays assigned by the
///     methods on <see cref="string"/>.
/// </remarks>
internal readonly struct LazyStringSplit : IEnumerable<string>
{
    private readonly string _input;
    private readonly char _delimiter;

    public LazyStringSplit(string input, char delimiter)
    {
        Requires.NotNull(input);

        _input = input;
        _delimiter = delimiter;
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

    public IEnumerable<T> Select<T>(Func<string, T> func)
    {
        foreach (string value in this)
        {
            yield return func(value);
        }
    }

    public string First()
    {
        return FirstOrDefault() ?? throw new InvalidOperationException("Sequence is empty.");
    }

    public string? FirstOrDefault()
    {
        var enumerator = new Enumerator(this);
        return enumerator.MoveNext() ? enumerator.Current : null;
    }

    public struct Enumerator : IEnumerator<string>
    {
        private readonly string _input;
        private readonly char _delimiter;
        private int _index;

        internal Enumerator(in LazyStringSplit split)
        {
            _index = 0;
            _input = split._input;
            _delimiter = split._delimiter;
            Current = null!;
        }

        public string Current { get; private set; }

        public bool MoveNext()
        {
            while (_index != _input.Length)
            {
                int delimiterIndex = _input.IndexOf(_delimiter, _index);

                if (delimiterIndex == -1)
                {
                    Current = _input.Substring(_index);
                    _index = _input.Length;
                    return true;
                }

                int length = delimiterIndex - _index;

                if (length == 0)
                {
                    _index++;
                    continue;
                }

                Current = _input.Substring(_index, length);
                _index = delimiterIndex + 1;
                return true;
            }

            return false;
        }

        object IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            _index = 0;
            Current = null!;
        }

        void IDisposable.Dispose() { }
    }
}

internal static class LazyStringSplitExtensions
{
    /// <remarks>
    ///     This extension method has special knowledge of the <see cref="LazyStringSplit"/> type and
    ///     can compute its result without allocation.
    /// </remarks>
    /// <inheritdoc cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
    public static string? FirstOrDefault(this LazyStringSplit lazyStringSplit)
    {
        LazyStringSplit.Enumerator enumerator = lazyStringSplit.GetEnumerator();

        return enumerator.MoveNext()
            ? enumerator.Current
            : null;
    }
}
