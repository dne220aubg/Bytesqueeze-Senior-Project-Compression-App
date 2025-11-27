using System;
using System.Collections.Generic;

namespace SeniorProjectCompressionApp.Models
{
    internal sealed class DynamicHuffmanModel
    {
        public DynamicHuffmanModel(HuffmanCode[] litLen, HuffmanCode[] dist, HuffmanCode[]? clCodes, int[]? clLengths, int hclenStored, int hlitCount, int hdistCount, List<int> lengthSymbols, List<int> lengthExtras, int totalBits, DecodeTable? litTable, DecodeTable? distTable, int codeLengthCount = 0, int hlitStored = 0, int hdistStored = 0)
        {
            LiteralLengthCodes = litLen; DistanceCodes = dist; CodeLengthCodes = clCodes; CodeLengthLengths = clLengths;
            HCLENStored = hclenStored; HLITStored = hlitStored; HDISTStored = hdistStored;
            CodeLengthCount = codeLengthCount; HLITCount = hlitCount; HDISTCount = hdistCount;
            LengthSymbols = lengthSymbols; LengthExtras = lengthExtras; TotalBits = totalBits;
            LiteralDecodeTable = litTable; DistanceDecodeTable = distTable;
        }
        public HuffmanCode[] LiteralLengthCodes { get; }
        public HuffmanCode[] DistanceCodes { get; }
        public HuffmanCode[]? CodeLengthCodes { get; }
        public int[]? CodeLengthLengths { get; }
        public int HCLENStored { get; }
        public int HLITStored { get; }
        public int HDISTStored { get; }
        public int CodeLengthCount { get; }
        public int HLITCount { get; }
        public int HDISTCount { get; }
        public List<int> LengthSymbols { get; }
        public List<int> LengthExtras { get; }
        public int TotalBits { get; }
        public DecodeTable? LiteralDecodeTable { get; }
        public DecodeTable? DistanceDecodeTable { get; }
    }
}
