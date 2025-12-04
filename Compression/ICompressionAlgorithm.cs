using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.Compression
{
    // Represents a compression algorithm that can transform raw data into a compact form and reverse the process.
    public interface ICompressionAlgorithm
    {
        // Display name shown in the UI.
        string Name { get; }

        Task CompressAsync(Stream input, Stream output, IProgress<long>? progress, CancellationToken cancellationToken);
        Task DecompressAsync(Stream input, Stream output, IProgress<long>? progress, CancellationToken cancellationToken);
    }
}
