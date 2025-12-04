using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.Decompression
{
    internal sealed class DeflateDecoder
    {
        private const int WindowSize = 32768;
        private static readonly int[] CodeLengthOrder = new int[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        private static readonly int[] s_lengthBases = new int[] { 3,4,5,6,7,8,9,10,11,13,15,17,19,23,27,31,35,43,51,59,67,83,99,115,131,163,195,227,258 };
        private static readonly int[] s_lengthExtras = new int[] { 0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0 };
        private static readonly int[] s_distanceBases = new int[] { 1,2,3,4,5,7,9,13,17,25,33,49,65,97,129,193,257,385,513,769,1025,1537,2049,3073,4097,6145,8193,12289,16385,24577 };
        private static readonly int[] s_distanceExtras = new int[] { 0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12,12,13,13 };

        private readonly HuffmanCode[] _fixedLitLenCodes;
        private readonly HuffmanCode[] _fixedDistCodes;
        private readonly DecodeTable _fixedLitDecode;
        private readonly DecodeTable _fixedDistDecode;

        public DeflateDecoder(HuffmanCode[] fixedLit, HuffmanCode[] fixedDist, DecodeTable fixedLitDecode, DecodeTable fixedDistDecode)
        {
            _fixedLitLenCodes = fixedLit;
            _fixedDistCodes = fixedDist;
            _fixedLitDecode = fixedLitDecode;
            _fixedDistDecode = fixedDistDecode;
        }

        public void Decompress(Stream input, Stream output, CancellationToken cancellationToken)
        {
            DeflateBitReader reader = new DeflateBitReader(input);
            byte[] window = new byte[WindowSize];
            int winPos = 0;
            long totalBytes = 0;
            const int WindowMask = WindowSize - 1;

            bool isFinal;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!reader.TryReadBits(1, out uint bfinal)) throw new InvalidOperationException("Unexpected end of compressed stream (BFINAL).");
                isFinal = bfinal == 1;

                if (!reader.TryReadBits(2, out uint btype)) throw new InvalidOperationException("Unexpected end of compressed stream (BTYPE).");

                if (btype == 0) // Stored
                {
                    reader.AlignToByte();
                    if (!reader.TryReadBits(16, out uint len) || !reader.TryReadBits(16, out uint nlen)) throw new InvalidOperationException("Unexpected end of stored block.");
                    if (((len ^ 0xFFFFu) & 0xFFFFu) != nlen) throw new InvalidOperationException($"Corrupt stored block length. Len={len}, NLen={nlen}");
                    
                    byte[] buffer = new byte[len];
                    int read = input.Read(buffer, 0, buffer.Length);
                    if (read != buffer.Length) throw new InvalidOperationException("Unexpected end of stored block data.");
                    
                    output.Write(buffer, 0, buffer.Length);
                    
                    // Update window
                    for (int i = 0; i < len; i++)
                    {
                        window[winPos] = buffer[i];
                        winPos = (winPos + 1) & WindowMask;
                    }
                    totalBytes += len;
                }
                else if (btype == 1) // Fixed
                {
                    DecompressBlock(reader, output, cancellationToken, _fixedLitLenCodes, _fixedDistCodes, _fixedLitDecode, _fixedDistDecode, window, ref winPos, ref totalBytes);
                }
                else if (btype == 2) // Dynamic
                {
                    DynamicHuffmanModel model = ReadDynamicModel(reader);
                    DecompressBlock(reader, output, cancellationToken, model.LiteralLengthCodes, model.DistanceCodes, model.LiteralDecodeTable!, model.DistanceDecodeTable!, window, ref winPos, ref totalBytes);
                }
                else throw new NotSupportedException("Unknown block type.");

            } while (!isFinal);
        }

        private void DecompressBlock(DeflateBitReader reader, Stream output, CancellationToken ct, HuffmanCode[] litLenCodes, HuffmanCode[] distCodes, DecodeTable litTable, DecodeTable distTable, byte[] window, ref int winPos, ref long totalBytes)
        {
            const int WindowMask = WindowSize - 1;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                HuffmanSymbol symbol = DecodeSymbolFast(reader, litLenCodes, litTable);
                if (symbol.Symbol == 256) break;

                if (symbol.Symbol < 256)
                {
                    byte literal = (byte)symbol.Symbol;
                    output.WriteByte(literal);
                    window[winPos] = literal;
                    winPos = (winPos + 1) & WindowMask;
                    totalBytes++;
                }
                else
                {
                    int length = DecodeLength(symbol.Symbol, reader);
                    HuffmanSymbol distSymbol = DecodeSymbolFast(reader, distCodes, distTable);
                    int distance = DecodeDistanceValue(distSymbol.Symbol, reader);
                    
                    if (distance <= 0 || distance > WindowSize) throw new InvalidOperationException($"Invalid distance: {distance}");
                    if (totalBytes < WindowSize && distance > totalBytes) throw new InvalidOperationException($"Invalid distance (before window full): {distance} > {totalBytes}");
                    
                    for (int i = 0; i < length; i++)
                    {
                        int srcIndex = (winPos - distance) & WindowMask;
                        byte value = window[srcIndex];
                        output.WriteByte(value);
                        window[winPos] = value;
                        winPos = (winPos + 1) & WindowMask;
                        totalBytes++;
                    }
                }
            }
        }

        private HuffmanSymbol DecodeSymbolFast(DeflateBitReader reader, HuffmanCode[] codes, DecodeTable table)
        {
            if (reader.TryPeekBits(table.TableBits, out uint bits))
            {
                int index = (int)(bits & table.Mask);
                byte entryBits = table.Lengths[index];
                if (entryBits > 0)
                {
                    reader.DropBits(entryBits);
                    return new HuffmanSymbol(table.Symbols[index]);
                }
            }
            return DecodeSymbolLinear(reader, codes);
        }

        private HuffmanSymbol DecodeSymbolLinear(DeflateBitReader reader, HuffmanCode[] codes)
        {
            for (int bitLen = 1; bitLen <= 15; bitLen++)
            {
                if (!reader.TryPeekBits(bitLen, out uint peek)) throw new InvalidOperationException("Unexpected end.");
                uint mask = (1u << bitLen) - 1;
                uint candidate = peek & mask;
                for (int i = 0; i < codes.Length; i++)
                {
                    if (codes[i].BitLength == bitLen && codes[i].Code == candidate)
                    {
                        reader.DropBits(bitLen);
                        return new HuffmanSymbol(i);
                    }
                }
            }
            throw new InvalidOperationException("Failed to decode symbol.");
        }

        private static int DecodeLength(int symbol, DeflateBitReader reader)
        {
            int index = symbol - 257;
            int baseLen = s_lengthBases[index];
            int extra = s_lengthExtras[index];
            if (extra == 0) return baseLen;
            if (!reader.TryReadBits(extra, out uint extraValue)) throw new InvalidOperationException("Unexpected end.");
            return baseLen + (int)extraValue;
        }

        private static int DecodeDistanceValue(int symbol, DeflateBitReader reader)
        {
            int baseDist = s_distanceBases[symbol];
            int extra = s_distanceExtras[symbol];
            if (extra == 0) return baseDist;
            if (!reader.TryReadBits(extra, out uint extraValue)) throw new InvalidOperationException("Unexpected end.");
            return baseDist + (int)extraValue;
        }

        private DynamicHuffmanModel ReadDynamicModel(DeflateBitReader reader)
        {
            if (!reader.TryReadBits(5, out uint hlitBits) || !reader.TryReadBits(5, out uint hdistBits) || !reader.TryReadBits(4, out uint hclenBits))
                throw new InvalidOperationException("Unexpected end of dynamic header.");

            int hlitCount = (int)(hlitBits + 257);
            int hdistCount = (int)(hdistBits + 1);
            int hclenCount = (int)(hclenBits + 4);

            int[] clLengths = new int[19];
            for (int i = 0; i < hclenCount; i++)
            {
                if (!reader.TryReadBits(3, out uint len)) throw new InvalidOperationException("Unexpected end.");
                clLengths[CodeLengthOrder[i]] = (int)len;
            }

            HuffmanCode[] clCodes = DeflateHelpers.BuildCanonicalCodes(clLengths);
            int totalCodes = hlitCount + hdistCount;
            int[] lengths = new int[totalCodes];
            int index = 0;
            while (index < totalCodes)
            {
                HuffmanSymbol sym = DecodeSymbolLinear(reader, clCodes);
                if (sym.Symbol <= 15) lengths[index++] = sym.Symbol;
                else if (sym.Symbol == 16)
                {
                    if (!reader.TryReadBits(2, out uint r)) throw new InvalidOperationException("Unexpected end.");
                    int repeat = (int)r + 3;
                    int prev = lengths[index - 1];
                    for (int k = 0; k < repeat && index < totalCodes; k++) lengths[index++] = prev;
                }
                else if (sym.Symbol == 17)
                {
                    if (!reader.TryReadBits(3, out uint r)) throw new InvalidOperationException("Unexpected end.");
                    int repeat = (int)r + 3;
                    for (int k = 0; k < repeat && index < totalCodes; k++) lengths[index++] = 0;
                }
                else if (sym.Symbol == 18)
                {
                    if (!reader.TryReadBits(7, out uint r)) throw new InvalidOperationException("Unexpected end.");
                    int repeat = (int)r + 11;
                    for (int k = 0; k < repeat && index < totalCodes; k++) lengths[index++] = 0;
                }
            }

            int[] litLengths = new int[286];
            int[] distLengths = new int[30];
            Array.Copy(lengths, 0, litLengths, 0, hlitCount);
            Array.Copy(lengths, hlitCount, distLengths, 0, hdistCount);

            if (litLengths[256] == 0) litLengths[256] = 1;
            if (AllZero(distLengths)) distLengths[0] = 1;

            HuffmanCode[] litCodes = DeflateHelpers.BuildCanonicalCodes(litLengths);
            HuffmanCode[] distCodes = DeflateHelpers.BuildCanonicalCodes(distLengths);
            return new DynamicHuffmanModel(
                litCodes,
                distCodes,
                null,
                null,
                0,
                hlitCount,
                hdistCount,
                new List<int>(),   //   Decoder never reads this so we just pass a new empty list -> lengthSymbols: empty but non-null
                new List<int>(),   // again lengthExtras: empty but non-null
                0,
                DeflateHelpers.BuildDecodeTable(litCodes, 10),
                DeflateHelpers.BuildDecodeTable(distCodes, 8));
        }

        private static bool AllZero(int[] values)
        {
            foreach (int v in values) if (v != 0) return false;
            return true;
        }
    }
}
