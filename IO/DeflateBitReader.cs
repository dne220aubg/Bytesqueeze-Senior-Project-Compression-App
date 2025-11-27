using System;
using System.IO;

namespace SeniorProjectCompressionApp.IO
{
    // Reads bits in Deflate (LSB-first) order from a stream.
    internal sealed class DeflateBitReader
    {
        private readonly Stream _stream;
        private uint _bitBuffer;
        private int _bitCount;

        public DeflateBitReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        private bool EnsureBits(int count)
        {
            while (_bitCount < count)
            {
                int next = _stream.ReadByte();
                if (next < 0)
                {
                    return false;
                }

                _bitBuffer |= (uint)(next << _bitCount);
                _bitCount += 8;
            }

            return true;
        }

        public bool TryReadBits(int count, out uint value)
        {
            if (!EnsureBits(count))
            {
                value = 0;
                return false;
            }

            value = _bitBuffer & ((1u << count) - 1);
            _bitBuffer >>= count;
            _bitCount -= count;
            return true;
        }

        public bool TryPeekBits(int count, out uint value)
        {
            if (!EnsureBits(count))
            {
                value = 0;
                return false;
            }

            value = _bitBuffer & ((1u << count) - 1);
            return true;
        }

        public void DropBits(int count)
        {
            if (!EnsureBits(count))
            {
                throw new EndOfStreamException("Unexpected end of compressed data.");
            }

            _bitBuffer >>= count;
            _bitCount -= count;
        }

        public void AlignToByte()
        {
            int drop = _bitCount % 8;
            if (drop > 0)
            {
                DropBits(drop);
            }
        }
    }
}
