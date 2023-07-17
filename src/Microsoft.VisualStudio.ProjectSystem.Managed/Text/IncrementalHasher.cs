// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    ///  Provides support for computing a hash for <see cref="string"/> instances
    ///  incrementally across several segments.
    /// </summary>
    internal class IncrementalHasher : IDisposable
    {
        private const int BufferCharacterSize = 631; // Largest amount of UTF-8 characters that can roughly fit in 2048 bytes
        private static readonly int s_bufferByteSize = Encoding.UTF8.GetMaxByteCount(BufferCharacterSize);
        private readonly IncrementalHash _hasher;
        private readonly byte[] _buffer;

        public IncrementalHasher()
        {
            _hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            _buffer = ArrayPool<byte>.Shared.Rent(s_bufferByteSize);
        }

        public void Append(string value)
        {
            Requires.NotNull(value);

            int charIndex = 0;
            while (charIndex < value.Length)
            {
                int charCount = Math.Min(BufferCharacterSize, value.Length - charIndex);

                int bytesCount = Encoding.UTF8.GetBytes(value, charIndex, charCount, _buffer, 0);
                charIndex += charCount;

                _hasher.AppendData(_buffer, 0, bytesCount);
            }
        }

        public Hash GetHashAndReset()
        {
            return new(_hasher.GetHashAndReset());
        }

        public void Dispose()
        {
            _hasher.Dispose();
            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }

    internal readonly struct Hash(byte[] bytes) : IEquatable<Hash>
    {
        private readonly byte[] _bytes = bytes;

        public override bool Equals(object obj) => obj is Hash hash && Equals(hash);
        
        public bool Equals(Hash other)
        {
            byte[] thisBytes = _bytes;
            byte[] thatBytes = other._bytes;

            if (ReferenceEquals(thisBytes, thatBytes))
            {
                return true;
            }

            if (thisBytes is null || thatBytes is null)
            {
                return false;
            }

            return thisBytes.AsSpan().SequenceEqual(thatBytes.AsSpan());
        }

        public override int GetHashCode()
        {
            const int prime = 0x1000193;

            unchecked
            {
                int hash = (int)0x811C9DC5;

                for (int i = 0; i < _bytes.Length; i++)
                {
                    hash = (hash ^ _bytes[i]) * prime;
                }

                return hash;
            }
        }
    }
}
