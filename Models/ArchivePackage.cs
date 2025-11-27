namespace SeniorProjectCompressionApp.Models
{
    // Wraps the manifest, compressed data, and encryption flag for persistence.
    public sealed class ArchivePackage
    {
        public ArchivePackage(ArchiveManifest manifest, CompressionResult compressionResult, bool isEncrypted)
        {
            Manifest = manifest;
            CompressionResult = compressionResult;
            IsEncrypted = isEncrypted;
        }

        // Manifest describing the archive contents.
        public ArchiveManifest Manifest { get; }

        // Compressed data plus metadata.
        public CompressionResult CompressionResult { get; }

        // Indicates whether the payload is encrypted.
        public bool IsEncrypted { get; }
    }
}
