using System;

namespace SeniorProjectCompressionApp.Models
{
    internal readonly struct HuffmanCode
    {
        public HuffmanCode(uint c, byte b) { Code = c; BitLength = b; }
        public uint Code { get; }
        public byte BitLength { get; }
    }
}
