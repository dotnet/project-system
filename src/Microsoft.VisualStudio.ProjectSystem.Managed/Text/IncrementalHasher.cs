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
            Requires.NotNull(value, nameof(value));

            int charIndex = 0;
            while (charIndex < value.Length)
            {
                int charCount = Math.Min(BufferCharacterSize, value.Length - charIndex);

                int bytesCount = Encoding.UTF8.GetBytes(value, charIndex, charCount, _buffer, 0);
                charIndex += charCount;

                _hasher.AppendData(_buffer, 0, bytesCount);
            }
        }

        public byte[] GetHashAndReset()
        {
            return _hasher.GetHashAndReset();
        }

        public void Dispose()
        {
            _hasher.Dispose();
            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}
