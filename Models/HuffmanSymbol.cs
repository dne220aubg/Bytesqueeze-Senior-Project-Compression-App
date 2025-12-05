using System;

namespace SeniorProjectCompressionApp.Models
{
    // Getter for a decoded Huffman symbol index (literal/length or distance).
    internal readonly struct HuffmanSymbol
    {
        public HuffmanSymbol(int s) { Symbol = s; }
        public int Symbol { get; }
    }
}
