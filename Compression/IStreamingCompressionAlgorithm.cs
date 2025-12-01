using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.Compression
{
    // Supports streaming compression and decompression to avoid buffering entire payloads in memory.
    public interface IStreamingCompressionAlgorithm : ICompressionAlgorithm
    {
        Task CompressAsync(Stream input, Stream output, IProgress<long>? progress, CancellationToken cancellationToken);
        Task DecompressAsync(Stream input, Stream output, IProgress<long>? progress, CancellationToken cancellationToken);
    }
}
