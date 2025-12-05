using System;

namespace SeniorProjectCompressionApp.Models
{
    // Huffman tree node used when building code lengths (symbol/frequency with optional children)
    internal sealed class Node
    {
        public Node(int s, int f, Node? l, Node? r) { Symbol = s; Frequency = f; Left = l; Right = r; }
        public int Symbol { get; }
        public int Frequency { get; }
        public Node? Left { get; }
        public Node? Right { get; }
    }
}