using SeniorProjectCompressionApp.Compression;

namespace SeniorProjectCompressionApp.Models
{
    // Bundles compressed data together with metadata needed to restore it.
    public sealed class CompressionResult
    {
        public CompressionResult(CompressionMetadata metadata, byte[] compressedData)
        {
            Metadata = metadata;
            CompressedData = compressedData;
        }

        // Metadata produced by the compression algorithm.
        public CompressionMetadata Metadata { get; }

        // Raw compressed binary payload.
        public byte[] CompressedData { get; }
    }
}
