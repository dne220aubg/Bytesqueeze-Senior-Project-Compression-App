using System;

namespace SeniorProjectCompressionApp.Models
{
    internal sealed class DecodeTable
    {
        public DecodeTable(int[] s, byte[] l, int t) { Symbols = s; Lengths = l; TableBits = t; Mask = (1 << t) - 1; }
        public int[] Symbols { get; }
        public byte[] Lengths { get; }
        public int TableBits { get; }
        public int Mask { get; }
    }
}
