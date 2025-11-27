using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.Compression.Algorithms
{
    internal sealed class DeflateEncoder
    {
        private const int WindowSize = 32768;
        private const int MinMatch = 3;
        private const int MaxMatch = 258;
        private const int HashSize = 1 << 15;
        private const int HashMask = HashSize - 1;
        private const int BlockSize = 64 * 1024;

        // Tunable parameters
        private readonly int _maxChain;
        private readonly int _niceMatch;
        private readonly bool _lazyMatching;

        private static readonly int[] CodeLengthOrder = new int[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        private static readonly int[] s_lengthBases = new int[] { 3,4,5,6,7,8,9,10,11,13,15,17,19,23,27,31,35,43,51,59,67,83,99,115,131,163,195,227,258 };
        private static readonly int[] s_lengthExtras = new int[] { 0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0 };
        private static readonly int[] s_distanceBases = new int[] { 1,2,3,4,5,7,9,13,17,25,33,49,65,97,129,193,257,385,513,769,1025,1537,2049,3073,4097,6145,8193,12289,16385,24577 };
        private static readonly int[] s_distanceExtras = new int[] { 0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12,12,13,13 };

        private readonly Stream _output;
        private readonly DeflateBitWriter _writer;
        private readonly HuffmanCode[] _fixedLitLenCodes;
        private readonly HuffmanCode[] _fixedDistCodes;

        private readonly byte[] _window;
        private readonly int[] _head;
        private readonly int[] _prev;
        
        private int _windowPos; 
        private int _blockStart; 
        private const int WindowMask = WindowSize - 1;
        private const int BufferSize = 2 * WindowSize + BlockSize; 

        public DeflateEncoder(Stream output, HuffmanCode[] fixedLit, HuffmanCode[] fixedDist, CompressionLevel level)
        {
            _output = output;
            _writer = new DeflateBitWriter();
            _fixedLitLenCodes = fixedLit;
            _fixedDistCodes = fixedDist;
            
            _window = new byte[BufferSize];
            _head = new int[HashSize];
            _prev = new int[WindowSize];
            
            for (int i = 0; i < _head.Length; i++)
                _head[i] = -1;

            // Tune parameters based on level
            switch (level)
            {
                case CompressionLevel.Fast:
                    _maxChain = 2;
                    _niceMatch = 16;
                    _lazyMatching = false;
                    break;
                case CompressionLevel.Best:
                    _maxChain = 32;
                    _niceMatch = 258;
                    _lazyMatching = true;
                    break;
                case CompressionLevel.Normal:
                default:
                    _maxChain = 4;
                    _niceMatch = 32;
                    _lazyMatching = true;
                    break;
            }
        }

        public void CompressBytes(byte[] data, int offset, int count, CancellationToken ct)
        {
            int inputPos = offset;
            int inputEnd = offset + count;
            int lookahead = 0;
            _windowPos = 0;

            while (inputPos < inputEnd || lookahead > 0)
            {
                int spaceAvailable = _window.Length - (_windowPos + lookahead);
                int toCopy = Math.Min(spaceAvailable, inputEnd - inputPos);
                
                if (toCopy > 0)
                {
                    Array.Copy(data, inputPos, _window, _windowPos + lookahead, toCopy);
                    inputPos += toCopy;
                    lookahead += toCopy;
                }

                if (lookahead == 0 && inputPos == inputEnd) break;

                ProcessBlock(ref lookahead, inputPos == inputEnd, ct);

                if (_windowPos >= WindowSize + BlockSize)
                {
                    ShiftWindow(ref lookahead);
                }
            }
            
            _writer.Finish(_output);
        }

        public async Task CompressStreamAsync(Stream input, CancellationToken ct)
        {
            int bytesRead;
            int lookahead = 0;
            _windowPos = 0;
            
            while (true)
            {
                int spaceAvailable = _window.Length - (_windowPos + lookahead);
                if (spaceAvailable > 0)
                {
                    bytesRead = await input.ReadAsync(_window, _windowPos + lookahead, spaceAvailable, ct).ConfigureAwait(false);
                    lookahead += bytesRead;
                }
                else
                {
                    bytesRead = 0;
                }

                if (lookahead == 0 && bytesRead == 0) break;

                ProcessBlock(ref lookahead, bytesRead == 0, ct);

                if (_windowPos >= WindowSize + BlockSize)
                {
                    ShiftWindow(ref lookahead);
                }
            }
            
            if (lookahead > 0)
            {
                    ProcessBlock(ref lookahead, true, ct);
            }
            
            _writer.Finish(_output);
        }

        private void ShiftWindow(ref int lookahead)
        {
            int shift = _windowPos - WindowSize;
            Array.Copy(_window, shift, _window, 0, WindowSize + lookahead);
            
            _windowPos -= shift;
            _blockStart -= shift;

            for (int i = 0; i < HashSize; i++)
            {
                int val = _head[i];
                _head[i] = val >= shift ? val - shift : -1;
            }
            
            for (int i = 0; i < WindowSize; i++)
            {
                int val = _prev[i];
                _prev[i] = val >= shift ? val - shift : -1;
            }
        }

        private void ProcessBlock(ref int lookahead, bool isFinalStream, CancellationToken ct)
        {
            List<Token> tokens = new List<Token>();
            int pos = _windowPos;
            int end = pos + lookahead;
            int limit = isFinalStream ? end : end - MaxMatch;
            
            if (limit <= pos) return;

            while (pos < limit)
            {
                ct.ThrowIfCancellationRequested();

                int remaining = end - pos;
                int bestLen = 0;
                int bestDist = 0;

                if (remaining >= MinMatch)
                {
                    int hash = ComputeHash(_window, pos);
                    int candidate = _head[hash];
                    _head[hash] = pos;
                    _prev[pos & WindowMask] = candidate;

                    if (candidate != -1 && pos - candidate <= WindowSize)
                    {
                        int chain = 0;
                        int curMatch = candidate;
                        
                        while (curMatch != -1 && chain < _maxChain && pos - curMatch <= WindowSize)
                        {
                            int len = MatchLength(_window, pos, curMatch, Math.Min(remaining, MaxMatch));
                            if (len > bestLen && len >= MinMatch)
                            {
                                bestLen = len;
                                bestDist = pos - curMatch;
                                if (bestLen == MaxMatch) break;
                                if (bestLen >= _niceMatch) break;
                            }
                            curMatch = _prev[curMatch & WindowMask];
                            chain++;
                        }
                    }
                }

                if (bestLen >= MinMatch)
                {
                    // Lazy Matching
                    if (_lazyMatching && remaining > MinMatch + 1 && bestLen < _niceMatch)
                    {
                            int nextHash = ComputeHash(_window, pos + 1);
                            int nextCandidate = _head[nextHash];
                            int nextBestLen = 0;
                            if (nextCandidate != -1 && (pos + 1) - nextCandidate <= WindowSize)
                            {
                                int chain = 0;
                                int curMatch = nextCandidate;
                                int nextRemaining = remaining - 1;
                                while (curMatch != -1 && chain < _maxChain && (pos + 1) - curMatch <= WindowSize)
                                {
                                    int len = MatchLength(_window, pos + 1, curMatch, Math.Min(nextRemaining, MaxMatch));
                                    if (len > nextBestLen)
                                    {
                                        nextBestLen = len;
                                        if (nextBestLen == MaxMatch) break;
                                        if (nextBestLen >= _niceMatch) break;
                                    }
                                    curMatch = _prev[curMatch & WindowMask];
                                    chain++;
                                }
                            }

                            if (nextBestLen > bestLen + 1)
                            {
                                tokens.Add(Token.ForLiteral(_window[pos]));
                                pos++;
                                continue;
                            }
                    }

                    GetLengthCode(bestLen, out int lenCode, out int lenExtraBits, out int lenExtraValue);
                    GetDistanceCode(bestDist, out int distCode, out int distExtraBits, out int distExtraValue);
                    tokens.Add(Token.ForMatch(bestLen, bestDist, lenCode, lenExtraBits, lenExtraValue, distCode, distExtraBits, distExtraValue));

                    int matchEnd = pos + bestLen;
                    for (int i = pos + 1; i < matchEnd && i < limit; i += 2)
                    {
                        int h = ComputeHash(_window, i);
                        _prev[i & WindowMask] = _head[h];
                        _head[h] = i;
                    }
                    pos += bestLen;
                }
                else
                {
                    tokens.Add(Token.ForLiteral(_window[pos]));
                    pos++;
                }
                
                if (tokens.Count >= BlockSize)
                {
                    WriteBlock(tokens, false);
                    tokens.Clear();
                }
            }

            _windowPos = pos;
            lookahead = end - pos;
            
            if (isFinalStream && lookahead == 0 && tokens.Count > 0)
            {
                WriteBlock(tokens, true);
                tokens.Clear();
            }
            else if (tokens.Count > 0)
            {
                    WriteBlock(tokens, isFinalStream && lookahead == 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int MatchLength(byte[] window, int s1, int s2, int limit)
        {
            int len = 0;
            int remaining = limit;
            
            while (remaining >= 4)
            {
                if (window[s1 + len] != window[s2 + len]) return len;
                if (window[s1 + len + 1] != window[s2 + len + 1]) return len + 1;
                if (window[s1 + len + 2] != window[s2 + len + 2]) return len + 2;
                if (window[s1 + len + 3] != window[s2 + len + 3]) return len + 3;
                len += 4;
                remaining -= 4;
            }
            
            while (remaining > 0 && window[s1 + len] == window[s2 + len])
            {
                len++;
                remaining--;
            }
            
            return len;
        }

        private void WriteBlock(List<Token> tokens, bool isFinal)
        {
            tokens.Add(Token.EndOfBlock());

            int storedBits = StoredBlockBitCount(tokens);
            int fixedBits = CalculateBlockBits(tokens, _fixedLitLenCodes, _fixedDistCodes, 3);
            DynamicHuffmanModel dynamicModel = BuildDynamicModel(tokens);
            int dynamicBits = dynamicModel.TotalBits;

            bool useStored = storedBits < Math.Min(fixedBits, dynamicBits);
            bool preferDynamic = dynamicBits < fixedBits;

            if (useStored)
            {
                if (preferDynamic) WriteDynamicBlock(tokens, dynamicModel, _output, isFinal, _writer);
                else WriteFixedBlock(tokens, _output, isFinal, _fixedLitLenCodes, _fixedDistCodes, _writer);
            }
            else if (preferDynamic)
            {
                WriteDynamicBlock(tokens, dynamicModel, _output, isFinal, _writer);
            }
            else
            {
                WriteFixedBlock(tokens, _output, isFinal, _fixedLitLenCodes, _fixedDistCodes, _writer);
            }
            
            _writer.Flush(_output);
        }
        
        private int StoredBlockBitCount(List<Token> tokens)
        {
            int bytes = 0;
            foreach(var t in tokens)
            {
                if (t.IsLiteral) bytes++;
                else if (!t.IsEndOfBlock) bytes += t.Length;
            }
            if (bytes > 65535) return int.MaxValue;
            return 32 + bytes * 8 + 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeHash(byte[] data, int position)
        {
            int h = (data[position] << 5) ^ data[position + 1];
            h = (h << 5) ^ data[position + 2];
            return h & HashMask;
        }

        private static void GetLengthCode(int length, out int code, out int extraBits, out int extraValue)
        {
            for (int i = 0; i < s_lengthBases.Length; i++)
            {
                int baseLen = s_lengthBases[i];
                int maxLen = baseLen + ((1 << s_lengthExtras[i]) - 1);
                if (length >= baseLen && length <= maxLen)
                {
                    code = 257 + i;
                    extraBits = s_lengthExtras[i];
                    extraValue = length - baseLen;
                    return;
                }
            }
            code = 285; extraBits = 0; extraValue = 0;
        }

        private static void GetDistanceCode(int distance, out int code, out int extraBits, out int extraValue)
        {
            for (int i = 0; i < s_distanceBases.Length; i++)
            {
                int baseDist = s_distanceBases[i];
                int maxDist = baseDist + ((1 << s_distanceExtras[i]) - 1);
                if (distance >= baseDist && distance <= maxDist)
                {
                    code = i;
                    extraBits = s_distanceExtras[i];
                    extraValue = distance - baseDist;
                    return;
                }
            }
            code = s_distanceBases.Length - 1;
            extraBits = s_distanceExtras[code];
            extraValue = distance - s_distanceBases[code];
        }

        private static int CalculateBlockBits(List<Token> tokens, HuffmanCode[] litCodes, HuffmanCode[] distCodes, int headerBits)
        {
            int bits = headerBits;
            foreach (Token t in tokens)
            {
                if (t.IsLiteral) bits += litCodes[t.LiteralValue].BitLength;
                else if (t.IsEndOfBlock) bits += litCodes[256].BitLength;
                else
                {
                    bits += litCodes[t.LengthCode].BitLength + t.LengthExtraBits;
                    bits += distCodes[t.DistanceCode].BitLength + t.DistanceExtraBits;
                }
            }
            return bits;
        }

        private static void WriteFixedBlock(List<Token> tokens, Stream output, bool isFinal, HuffmanCode[] litCodes, HuffmanCode[] distCodes, DeflateBitWriter writer)
        {
            writer.WriteBits(isFinal ? 1u : 0u, 1);
            writer.WriteBits(1, 2);
            WriteTokens(tokens, litCodes, distCodes, writer);
        }

        private static void WriteDynamicBlock(List<Token> tokens, DynamicHuffmanModel model, Stream output, bool isFinal, DeflateBitWriter writer)
        {
            writer.WriteBits(isFinal ? 1u : 0u, 1);
            writer.WriteBits(2, 2);

            writer.WriteBits((uint)model.HLITStored, 5);
            writer.WriteBits((uint)model.HDISTStored, 5);
            writer.WriteBits((uint)model.HCLENStored, 4);

            for (int i = 0; i < model.CodeLengthCount; i++)
            {
                int len = model.CodeLengthLengths![CodeLengthOrder[i]];
                writer.WriteBits((uint)len, 3);
            }

            for (int i = 0; i < model.LengthSymbols.Count; i++)
            {
                int sym = model.LengthSymbols[i];
                int extra = model.LengthExtras[i];
                HuffmanCode code = model.CodeLengthCodes![sym];
                writer.WriteBits(code.Code, code.BitLength);
                if (sym == 16) writer.WriteBits((uint)extra, 2);
                else if (sym == 17) writer.WriteBits((uint)extra, 3);
                else if (sym == 18) writer.WriteBits((uint)extra, 7);
            }

            WriteTokens(tokens, model.LiteralLengthCodes, model.DistanceCodes, writer);
        }

        private static void WriteTokens(List<Token> tokens, HuffmanCode[] litCodes, HuffmanCode[] distCodes, DeflateBitWriter writer)
        {
            foreach (Token t in tokens)
            {
                if (t.IsLiteral)
                {
                    HuffmanCode c = litCodes[t.LiteralValue];
                    writer.WriteBits(c.Code, c.BitLength);
                }
                else if (t.IsEndOfBlock)
                {
                    HuffmanCode c = litCodes[256];
                    writer.WriteBits(c.Code, c.BitLength);
                }
                else
                {
                    HuffmanCode lenCode = litCodes[t.LengthCode];
                    writer.WriteBits(lenCode.Code, lenCode.BitLength);
                    if (t.LengthExtraBits > 0) writer.WriteBits((uint)t.LengthExtraValue, t.LengthExtraBits);

                    HuffmanCode distCode = distCodes[t.DistanceCode];
                    writer.WriteBits(distCode.Code, distCode.BitLength);
                    if (t.DistanceExtraBits > 0) writer.WriteBits((uint)t.DistanceExtraValue, t.DistanceExtraBits);
                }
            }
        }

        private static DynamicHuffmanModel BuildDynamicModel(List<Token> tokens)
        {
            int[] litFreq = new int[286];
            int[] distFreq = new int[30];

            foreach (Token t in tokens)
            {
                if (t.IsLiteral) litFreq[t.LiteralValue]++;
                else if (t.IsEndOfBlock) litFreq[256]++;
                else
                {
                    litFreq[t.LengthCode]++;
                    distFreq[t.DistanceCode]++;
                }
            }

            if (AllZero(distFreq)) distFreq[0] = 1;

            int[] litLengths = BuildCodeLengths(litFreq, 15);
            int[] distLengths = BuildCodeLengths(distFreq, 15);

            HuffmanCode[] litCodes = DeflateHelpers.BuildCanonicalCodes(litLengths);
            HuffmanCode[] distCodes = DeflateHelpers.BuildCanonicalCodes(distLengths);

            List<int> lengthSymbols = new List<int>();
            List<int> lengthExtras = new List<int>();

            int hlitCount = TrimTrailingZeros(litLengths, 257);
            int hdistCount = TrimTrailingZeros(distLengths, 1);

            List<int> allLengths = new List<int>(hlitCount + hdistCount);
            allLengths.AddRange(SubArray(litLengths, hlitCount));
            allLengths.AddRange(SubArray(distLengths, hdistCount));

            RleCodeLengths(allLengths, lengthSymbols, lengthExtras);

            int[] clFreq = new int[19];
            foreach (int sym in lengthSymbols) clFreq[sym]++;

            int[] clLengths = BuildCodeLengths(clFreq, 7);
            int clLast = TrimTrailingZerosOrder(clLengths);
            int clCount = clLast + 1;
            int hclenStored = Math.Max(0, clCount - 4);

            HuffmanCode[] clCodes = DeflateHelpers.BuildCanonicalCodes(clLengths);

            int headerBits = 3 + 5 + 5 + 4;
            for (int i = 0; i < clCount; i++) headerBits += clLengths[CodeLengthOrder[i]];

            int lengthsBits = 0;
            for (int i = 0; i < lengthSymbols.Count; i++)
            {
                int sym = lengthSymbols[i];
                int extra = lengthExtras[i];
                lengthsBits += clCodes[sym].BitLength;
                if (sym == 16) lengthsBits += 2;
                else if (sym == 17) lengthsBits += 3;
                else if (sym == 18) lengthsBits += 7;
            }

            int payloadBits = CalculateBlockBits(tokens, litCodes, distCodes, 0);
            
            return new DynamicHuffmanModel(
                litCodes, distCodes, clCodes, clLengths, hclenStored, hlitCount, hdistCount,
                lengthSymbols, lengthExtras, headerBits + lengthsBits + payloadBits, null, null, clCount, hlitCount - 257, hdistCount - 1);
        }

        private static int[] BuildCodeLengths(int[] frequencies, int maxBits)
        {
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < frequencies.Length; i++) if (frequencies[i] > 0) nodes.Add(new Node(i, frequencies[i], null, null));
            
            if (nodes.Count < 2)
            {
                for (int i = 0; i < frequencies.Length && nodes.Count < 2; i++)
                {
                    if (frequencies[i] == 0)
                    {
                        nodes.Add(new Node(i, 1, null, null));
                    }
                }
            }

            while (nodes.Count > 1)
            {
                nodes.Sort((a, b) => { int cmp = a.Frequency.CompareTo(b.Frequency); return cmp != 0 ? cmp : a.Symbol.CompareTo(b.Symbol); });
                Node left = nodes[0]; Node right = nodes[1];
                nodes.RemoveAt(0); nodes.RemoveAt(0);
                nodes.Add(new Node(-1, left.Frequency + right.Frequency, left, right));
            }
            int[] lengths = new int[frequencies.Length];
            AssignLengths(nodes[0], 0, lengths);

            long totalWeight = 0;
            long maxWeight = 1L << maxBits;
            
            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] > 0)
                {
                    if (lengths[i] > maxBits) lengths[i] = maxBits;
                    totalWeight += 1L << (maxBits - lengths[i]);
                }
            }

            if (totalWeight <= maxWeight) return lengths;

            var symbols = new List<(int Symbol, int Frequency)>();
            for (int i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] > 0) symbols.Add((i, frequencies[i]));
            }
            
            symbols.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
            
            while (totalWeight > maxWeight)
            {
                bool found = false;
                for (int i = 0; i < symbols.Count; i++)
                {
                    int sym = symbols[i].Symbol;
                    if (lengths[sym] < maxBits)
                    {
                        lengths[sym]++;
                        totalWeight -= (1L << (maxBits - lengths[sym])); 
                        found = true;
                        if (totalWeight <= maxWeight) break;
                    }
                }
                if (!found) break; 
            }
            
            return lengths;
        }

        private static void AssignLengths(Node node, int depth, int[] lengths)
        {
            if (node.Left == null && node.Right == null) { if (node.Symbol >= 0) lengths[node.Symbol] = Math.Max(1, depth); return; }
            if (node.Left != null) AssignLengths(node.Left, depth + 1, lengths);
            if (node.Right != null) AssignLengths(node.Right, depth + 1, lengths);
        }

        private static bool AllZero(int[] values) { foreach (int v in values) if (v != 0) return false; return true; }
        private static int TrimTrailingZeros(int[] lengths, int minCount) { int last = lengths.Length - 1; while (last >= minCount && lengths[last] == 0) last--; return last + 1; }
        private static int TrimTrailingZerosOrder(int[] lengths) { int last = lengths.Length - 1; while (last >= 0 && lengths[CodeLengthOrder[last]] == 0) last--; return last; }
        private static int[] SubArray(int[] source, int length) { int[] r = new int[length]; Array.Copy(source, r, length); return r; }

        private static void RleCodeLengths(List<int> lengths, List<int> symbols, List<int> extras)
        {
            int i = 0;
            while (i < lengths.Count)
            {
                int val = lengths[i]; int run = 1;
                while (i + run < lengths.Count && lengths[i + run] == val) run++;
                int origRun = run;
                if (val == 0)
                {
                    while (run > 0)
                    {
                        if (run >= 11) { int use = Math.Min(run, 138); symbols.Add(18); extras.Add(use - 11); run -= use; }
                        else if (run >= 3) { int use = Math.Min(run, 10); symbols.Add(17); extras.Add(use - 3); run -= use; }
                        else { symbols.Add(0); extras.Add(0); run--; }
                    }
                }
                else
                {
                    symbols.Add(val); extras.Add(0); run--;
                    while (run > 0)
                    {
                        if (run >= 3) { int use = Math.Min(run, 6); symbols.Add(16); extras.Add(use - 3); run -= use; }
                        else { symbols.Add(val); extras.Add(0); run--; }
                    }
                }
                i += origRun;
            }
        }
    }
}
