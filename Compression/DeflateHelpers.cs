using System;
using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.Compression
{
    internal static class DeflateHelpers
    {
        public static HuffmanCode[] BuildFixedLiteralLengthCodes()
        {
            int[] lengths = new int[288];
            for (int i = 0; i <= 143; i++) lengths[i] = 8;
            for (int i = 144; i <= 255; i++) lengths[i] = 9;
            for (int i = 256; i <= 279; i++) lengths[i] = 7;
            for (int i = 280; i <= 287; i++) lengths[i] = 8;
            return BuildCanonicalCodes(lengths);
        }

        public static HuffmanCode[] BuildFixedDistanceCodes()
        {
            int[] lengths = new int[32];
            for (int i = 0; i < lengths.Length; i++) lengths[i] = 5;
            return BuildCanonicalCodes(lengths);
        }

        public static HuffmanCode[] BuildCanonicalCodes(int[] bitLengths)
        {
            int maxBits = 0;
            for (int i = 0; i < bitLengths.Length; i++) if (bitLengths[i] > maxBits) maxBits = bitLengths[i];

            int[] blCount = new int[maxBits + 1];
            for (int i = 0; i < bitLengths.Length; i++) if (bitLengths[i] > 0) blCount[bitLengths[i]]++;

            int code = 0;
            int[] nextCode = new int[maxBits + 1];
            for (int bits = 1; bits <= maxBits; bits++)
            {
                code = (code + blCount[bits - 1]) << 1;
                nextCode[bits] = code;
            }

            HuffmanCode[] table = new HuffmanCode[bitLengths.Length];
            for (int n = 0; n < bitLengths.Length; n++)
            {
                int len = bitLengths[n];
                if (len == 0) continue;
                table[n] = new HuffmanCode(ReverseBits((uint)nextCode[len]++, len), (byte)len);
            }
            return table;
        }

        public static uint ReverseBits(uint value, int bitLength)
        {
            uint reversed = 0;
            for (int i = 0; i < bitLength; i++)
            {
                reversed = (reversed << 1) | (value & 1u);
                value >>= 1;
            }
            return reversed;
        }

        public static DecodeTable BuildDecodeTable(HuffmanCode[] codes, int tableBits)
        {
            int tableSize = 1 << tableBits;
            int[] symbols = new int[tableSize];
            byte[] lengths = new byte[tableSize];
            for (int symbol = 0; symbol < codes.Length; symbol++)
            {
                HuffmanCode code = codes[symbol];
                if (code.BitLength == 0 || code.BitLength > tableBits) continue;
                int prefix = (int)code.Code;
                int step = 1 << code.BitLength;
                for (int i = prefix; i < tableSize; i += step)
                {
                    symbols[i] = symbol;
                    lengths[i] = code.BitLength;
                }
            }
            return new DecodeTable(symbols, lengths, tableBits);
        }
    }
}
