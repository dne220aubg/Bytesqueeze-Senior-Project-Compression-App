using System;

namespace SeniorProjectCompressionApp.Models
{
    // Represents a canonical Huffman code: bit pattern (least significant bit first for Deflate) and its bit length.
    internal readonly struct HuffmanCode
    {
        public HuffmanCode(uint c, byte b) { Code = c; BitLength = b; }
        public uint Code { get; }
        public byte BitLength { get; }
    }
}
