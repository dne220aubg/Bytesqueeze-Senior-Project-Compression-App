using System.Threading;

using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.Compression
{
    // Represents a compression algorithm that can transform raw data into a compact form and reverse the process.
    public interface ICompressionAlgorithm
    {
        // Display name shown in the UI.
        string Name { get; }

        // Compresses the payload and returns the metadata needed to restore it later.
        CompressionResult Compress(byte[] data, CancellationToken cancellationToken);

        // Reverses the compression using the provided metadata and compressed bytes.
        byte[] Decompress(CompressionMetadata metadata, byte[] compressedData, CancellationToken cancellationToken);
    }
}
