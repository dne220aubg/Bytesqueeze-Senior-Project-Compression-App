using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Decompression;
using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.Compression.Algorithms
{
    // A minimal RFC1951-style deflate implementation using fixed Huffman blocks (streaming-capable).
    public sealed class DeflateRfc1951Algorithm : IStreamingCompressionAlgorithm
    {
        private const int ParallelBlockSize = 4 * 1024 * 1024; // 4 MB chunks
        private static readonly byte[] MagicHeader = { 0x50, 0x44, 0x45, 0x46 }; // PDEF

        private readonly HuffmanCode[] _fixedLitLenCodes;
        private readonly HuffmanCode[] _fixedDistCodes;
        private readonly DecodeTable _fixedLitDecode;
        private readonly DecodeTable _fixedDistDecode;
        private readonly CompressionLevel _level;

        public string Name => _level.ToString();

        public DeflateRfc1951Algorithm(CompressionLevel level = CompressionLevel.Normal)
        {
            _level = level;
            _fixedLitLenCodes = DeflateHelpers.BuildFixedLiteralLengthCodes();
            _fixedDistCodes = DeflateHelpers.BuildFixedDistanceCodes();
            _fixedLitDecode = DeflateHelpers.BuildDecodeTable(_fixedLitLenCodes, 10);
            _fixedDistDecode = DeflateHelpers.BuildDecodeTable(_fixedDistCodes, 8);
        }

        public CompressionResult Compress(byte[] data, CancellationToken cancellationToken)
        {
            return Compress(data, 0, data.Length, cancellationToken);
        }

        public CompressionResult Compress(byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            // Output buffer estimate
            using (MemoryStream output = new MemoryStream(count / 2 + 256))
            {
                var encoder = new DeflateEncoder(output, _fixedLitLenCodes, _fixedDistCodes, _level);
                encoder.CompressBytes(data, offset, count, cancellationToken);
                return new CompressionResult(new CompressionMetadata(Name, count, new Dictionary<string, string>()), output.ToArray());
            }
        }

        public async Task CompressAsync(Stream input, Stream output, CancellationToken cancellationToken)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));

            // Write Header
            await output.WriteAsync(MagicHeader, 0, MagicHeader.Length, cancellationToken).ConfigureAwait(false);
            await output.WriteAsync(new byte[] { 1 }, 0, 1, cancellationToken).ConfigureAwait(false); // Version 1

            var tasks = new Queue<Task<byte[]>>();
            
            // Limit parallelism to ProcessorCount - 1 to keep system responsive (similar to WinRAR)
            int maxTasks = Math.Max(1, Environment.ProcessorCount - 1);
            
            while (true)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(ParallelBlockSize);
                int bytesRead; 
                try
                {
                    bytesRead = await input.ReadAsync(buffer, 0, ParallelBlockSize, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    throw;
                }

                if (bytesRead == 0)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    break;
                }

                int count = bytesRead;

                var task = Task.Run(() => 
                {
                    try
                    {
                        var result = Compress(buffer, 0, count, cancellationToken);
                        return result.CompressedData;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }, cancellationToken);

                tasks.Enqueue(task);

                if (tasks.Count >= maxTasks)
                {
                    await WriteNextBlockAsync(tasks.Dequeue(), output, cancellationToken).ConfigureAwait(false);
                }
            }

            while (tasks.Count > 0)
            {
                await WriteNextBlockAsync(tasks.Dequeue(), output, cancellationToken).ConfigureAwait(false);
            }

            // Write End of Stream Marker (Length 0)
            await output.WriteAsync(BitConverter.GetBytes(0), 0, 4, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteNextBlockAsync(Task<byte[]> task, Stream output, CancellationToken cancellationToken)
        {
            byte[] compressedBlock = await task.ConfigureAwait(false);
            
            byte[] lengthBytes = BitConverter.GetBytes(compressedBlock.Length);
            await output.WriteAsync(lengthBytes, 0, 4, cancellationToken).ConfigureAwait(false);
            
            await output.WriteAsync(compressedBlock, 0, compressedBlock.Length, cancellationToken).ConfigureAwait(false);
        }

        public byte[] Decompress(CompressionMetadata metadata, byte[] compressedData, CancellationToken cancellationToken)
        {
            if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));
            using (MemoryStream input = new MemoryStream(compressedData, writable: false))
            using (MemoryStream output = new MemoryStream(metadata?.OriginalSize ?? compressedData.Length * 2))
            {
                var decompressor = new DeflateDecompressor(_fixedLitLenCodes, _fixedDistCodes, _fixedLitDecode, _fixedDistDecode);
                decompressor.Decompress(input, output, cancellationToken);
                return output.ToArray();
            }
        }

        public async Task DecompressAsync(Stream input, Stream output, CancellationToken cancellationToken)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));

            byte[] header = new byte[MagicHeader.Length];
            int read;
            
            if (input.CanSeek)
            {
                long startPos = input.Position;
                read = await ReadFullAsync(input, header, cancellationToken).ConfigureAwait(false);
                
                if (read != header.Length || !header.SequenceEqual(MagicHeader))
                {
                    input.Position = startPos;
                    var decompressor = new DeflateDecompressor(_fixedLitLenCodes, _fixedDistCodes, _fixedLitDecode, _fixedDistDecode);
                    await Task.Run(() => decompressor.Decompress(input, output, cancellationToken), cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            else
            {
                read = await ReadFullAsync(input, header, cancellationToken).ConfigureAwait(false);
                if (read != header.Length || !header.SequenceEqual(MagicHeader))
                {
                     throw new NotSupportedException("Non-seekable stream does not contain expected Parallel Deflate header.");
                }
            }

            // Parallel Block Format
            {
                byte[] verBuf = new byte[1];
                await ReadFullAsync(input, verBuf, cancellationToken).ConfigureAwait(false);
                int version = verBuf[0];

                if (version != 1) throw new InvalidDataException($"Unsupported version: {version}");

                byte[] lengthBuffer = new byte[4];
                var tasks = new Queue<Task<byte[]>>();
                int maxTasks = Math.Max(1, Environment.ProcessorCount - 1);
                while (true)
                {
                    read = await ReadFullAsync(input, lengthBuffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break; 
                    if (read != 4) throw new EndOfStreamException();

                    int blockLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (blockLength == 0) break; // End Marker

                    byte[] compressedBlock = new byte[blockLength];
                    read = await ReadFullAsync(input, compressedBlock, cancellationToken).ConfigureAwait(false);
                    if (read != blockLength) throw new EndOfStreamException();

                    var task = Task.Run(() => 
                    {
                        using (var msInput = new MemoryStream(compressedBlock))
                        using (var msOutput = new MemoryStream(compressedBlock.Length * 4)) 
                        {
                            var decompressor = new DeflateDecompressor(_fixedLitLenCodes, _fixedDistCodes, _fixedLitDecode, _fixedDistDecode);
                            decompressor.Decompress(msInput, msOutput, cancellationToken);
                            return msOutput.ToArray();
                        }
                    }, cancellationToken);

                    tasks.Enqueue(task);

                    if (tasks.Count >= maxTasks)
                    {
                        byte[] decompressedBlock = await tasks.Dequeue().ConfigureAwait(false);
                        await output.WriteAsync(decompressedBlock, 0, decompressedBlock.Length, cancellationToken).ConfigureAwait(false);
                    }
                }

                while (tasks.Count > 0)
                {
                    byte[] decompressedBlock = await tasks.Dequeue().ConfigureAwait(false);
                    await output.WriteAsync(decompressedBlock, 0, decompressedBlock.Length, cancellationToken).ConfigureAwait(false);
                }
            }
            
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> ReadFullAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, token).ConfigureAwait(false);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }
    }
}
