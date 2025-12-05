using System;
using System.Collections.Generic;
using System.IO;

namespace SeniorProjectCompressionApp.IO
{
    // Writes bits in Deflate (least significant bit first) order.
    internal sealed class DeflateBitWriter
    {
        private readonly List<byte> _bytes = new List<byte>(1024);
        private uint _bitBuffer;
        private int _bitCount;

        public void WriteBits(uint value, int bitCount)
        {
            if ((uint)bitCount > 32 || bitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Bit count must be between 1 and 32.");
            }

            _bitBuffer |= (value & ((1u << bitCount) - 1)) << _bitCount;
            _bitCount += bitCount;

            while (_bitCount >= 8)
            {
                _bytes.Add((byte)_bitBuffer);
                _bitBuffer >>= 8;
                _bitCount -= 8;
            }
        }

        // Aligns to the next byte boundary by discarding pending bits.
        public void AlignToByte()
        {
            if (_bitCount > 0)
            {
                _bytes.Add((byte)_bitBuffer);
                _bitBuffer = 0;
                _bitCount = 0;
            }
        }

        public byte[] ToArray()
        {
            if (_bitCount > 0)
            {
                _bytes.Add((byte)_bitBuffer);
                _bitBuffer = 0;
                _bitCount = 0;
            }

            return _bytes.ToArray();
        }

        public void CopyTo(List<byte> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (_bitCount > 0)
            {
                _bytes.Add((byte)_bitBuffer);
                _bitBuffer = 0;
                _bitCount = 0;
            }
            target.AddRange(_bytes);
        }

        public void Flush(Stream output)
        {
            if (_bytes.Count > 0)
            {
                byte[] array = _bytes.ToArray();
                output.Write(array, 0, array.Length);
                _bytes.Clear();
            }
        }

        public void Finish(Stream output)
        {
            if (_bitCount > 0)
            {
                _bytes.Add((byte)_bitBuffer);
                _bitBuffer = 0;
                _bitCount = 0;
            }
            Flush(output);
        }
    }
}
