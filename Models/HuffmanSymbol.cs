using System;

namespace SeniorProjectCompressionApp.Models
{
    internal readonly struct HuffmanSymbol
    {
        public HuffmanSymbol(int s) { Symbol = s; }
        public int Symbol { get; }
    }
}
