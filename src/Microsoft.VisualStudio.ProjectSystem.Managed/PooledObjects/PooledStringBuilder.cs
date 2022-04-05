// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    /// <summary>
    /// The usage is:
    ///        var sb = PooledStringBuilder.GetInstance();
    ///        ... Do Stuff...
    ///        sb.ToStringAndFree()
    /// </summary>
    internal class PooledStringBuilder
    {
        private readonly StringBuilder _builder = new();
        private readonly ObjectPool<PooledStringBuilder> _pool;

        private PooledStringBuilder(ObjectPool<PooledStringBuilder> pool)
        {
            Requires.NotNull(pool, nameof(pool));
            _pool = pool;
        }

        public int Length { get => _builder.Length; set => _builder.Length = value; }

        public void Free()
        {
            StringBuilder builder = _builder;

            // do not store builders that are too large.
            if (builder.Capacity <= 1024)
            {
                builder.Clear();
                _pool.Free(this);
            }
        }

        public string ToStringAndFree()
        {
            string result = _builder.ToString();
            Free();

            return result;
        }

        public string ToStringAndFree(int startIndex, int length)
        {
            string result = _builder.ToString(startIndex, length);
            Free();

            return result;
        }

        // global pool
        private static readonly ObjectPool<PooledStringBuilder> s_poolInstance = CreatePool();

        /// <summary>
        /// If someone need to create a private pool
        /// </summary>
        /// <param name="size">The size of the pool.</param>
        public static ObjectPool<PooledStringBuilder> CreatePool(int size = 32)
        {
            ObjectPool<PooledStringBuilder>? pool = null;
            pool = new ObjectPool<PooledStringBuilder>(() => new PooledStringBuilder(pool!), size);
            return pool;
        }

        public static PooledStringBuilder GetInstance()
        {
            PooledStringBuilder builder = s_poolInstance.Allocate();
            //Debug.Assert(builder.Builder.Length == 0);
            return builder;
        }

        public static implicit operator StringBuilder(PooledStringBuilder obj) => obj._builder;

        public char this[int index] { get => _builder[index]; set => _builder[index] = value; }
        public void Append(double value) => _builder.Append(value);
        public void Append(char[] value) => _builder.Append(value);
        public void Append(object value) => _builder.Append(value);
        public void Append(ulong value) => _builder.Append(value);
        public void Append(uint value) => _builder.Append(value);
        public void Append(ushort value) => _builder.Append(value);
        public void Append(decimal value) => _builder.Append(value);
        public void Append(float value) => _builder.Append(value);
        public void Append(int value) => _builder.Append(value);
        public void Append(short value) => _builder.Append(value);
        public void Append(char value) => _builder.Append(value);
        public void Append(long value) => _builder.Append(value);
        public void Append(sbyte value) => _builder.Append(value);
        public void Append(byte value) => _builder.Append(value);
        public void Append(char[] value, int startIndex, int charCount) => _builder.Append(value, startIndex, charCount);
        public void Append(string value) => _builder.Append(value);
        public void Append(string value, int startIndex, int count) => _builder.Append(value, startIndex, count);
        public void Append(char value, int repeatCount) => _builder.Append(value, repeatCount);
        public void Append(bool value) => _builder.Append(value);
        public void AppendFormat(IFormatProvider provider, string format, params object[] args) => _builder.AppendFormat(provider, format, args);
        public void AppendFormat(string format, object arg0, object arg1, object arg2) => _builder.AppendFormat(format, arg0, arg1, arg2);
        public void AppendFormat(string format, params object[] args) => _builder.AppendFormat(format, args);
        public void AppendFormat(IFormatProvider provider, string format, object arg0) => _builder.AppendFormat(provider, format, arg0);
        public void AppendFormat(IFormatProvider provider, string format, object arg0, object arg1) => _builder.AppendFormat(provider, format, arg0, arg1);
        public void AppendFormat(IFormatProvider provider, string format, object arg0, object arg1, object arg2) => _builder.AppendFormat(provider, format, arg0, arg1, arg2);
        public void AppendFormat(string format, object arg0) => _builder.AppendFormat(format, arg0);
        public void AppendFormat(string format, object arg0, object arg1) => _builder.AppendFormat(format, arg0, arg1);
        public void AppendLine() => _builder.AppendLine();
        public void AppendLine(string value) => _builder.AppendLine(value);
        public void Clear() => _builder.Clear();
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => _builder.CopyTo(sourceIndex, destination, destinationIndex, count);
        public int EnsureCapacity(int capacity) => _builder.EnsureCapacity(capacity);
        public bool Equals(StringBuilder sb) => _builder.Equals(sb);
        public void Insert(int index, object value) => _builder.Insert(index, value);
        public void Insert(int index, byte value) => _builder.Insert(index, value);
        public void Insert(int index, ulong value) => _builder.Insert(index, value);
        public void Insert(int index, uint value) => _builder.Insert(index, value);
        public void Insert(int index, string value) => _builder.Insert(index, value);
        public void Insert(int index, decimal value) => _builder.Insert(index, value);
        public void Insert(int index, string value, int count) => _builder.Insert(index, value, count);
        public void Insert(int index, bool value) => _builder.Insert(index, value);
        public void Insert(int index, ushort value) => _builder.Insert(index, value);
        public void Insert(int index, short value) => _builder.Insert(index, value);
        public void Insert(int index, char value) => _builder.Insert(index, value);
        public void Insert(int index, sbyte value) => _builder.Insert(index, value);
        public void Insert(int index, char[] value, int startIndex, int charCount) => _builder.Insert(index, value, startIndex, charCount);
        public void Insert(int index, int value) => _builder.Insert(index, value);
        public void Insert(int index, long value) => _builder.Insert(index, value);
        public void Insert(int index, float value) => _builder.Insert(index, value);
        public void Insert(int index, double value) => _builder.Insert(index, value);
        public void Insert(int index, char[] value) => _builder.Insert(index, value);
        public void Remove(int startIndex, int length) => _builder.Remove(startIndex, length);
        public void Replace(string oldValue, string newValue) => _builder.Replace(oldValue, newValue);
        public void Replace(string oldValue, string newValue, int startIndex, int count) => _builder.Replace(oldValue, newValue, startIndex, count);
        public void Replace(char oldChar, char newChar) => _builder.Replace(oldChar, newChar);
        public void Replace(char oldChar, char newChar, int startIndex, int count) => _builder.Replace(oldChar, newChar, startIndex, count);
    }
}
