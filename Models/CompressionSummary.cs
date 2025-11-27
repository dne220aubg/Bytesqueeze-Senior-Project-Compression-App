using System;

namespace SeniorProjectCompressionApp.Models
{
    // Captures high-level statistics about a completed compression run.
    public sealed class CompressionSummary
    {
        public CompressionSummary(
            string outputPath,
            string algorithmName,
            long originalBytes,
            long archiveBytes,
            int compressedFileCount,
            bool wasEncrypted,
            long elapsedMilliseconds)
        {
            OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            AlgorithmName = algorithmName ?? string.Empty;
            OriginalBytes = Math.Max(0, originalBytes);
            ArchiveBytes = Math.Max(0, archiveBytes);
            CompressedFileCount = Math.Max(0, compressedFileCount);
            WasEncrypted = wasEncrypted;
            ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds);
        }

        // Location of the archive that was written to disk.
        public string OutputPath { get; }

        // Name of the algorithm that produced the compressed data.
        public string AlgorithmName { get; }

        // Number of bytes represented by the original input payload.
        public long OriginalBytes { get; }

        // Total number of bytes written to the archive file.
        public long ArchiveBytes { get; }

        // Number of file entries captured in the archive (directories excluded).
        public int CompressedFileCount { get; }

        // Indicates whether the archive payload was encrypted with a password.
        public bool WasEncrypted { get; }

        // Total time spent inside the compression algorithm, in milliseconds.
        public long ElapsedMilliseconds { get; }

        // Ratio of archive bytes to original bytes; returns 1 when original size is zero.
        public double CompressionRatio
        {
            get
            {
                if (OriginalBytes == 0)
                {
                    return 1.0;
                }

                return (double)ArchiveBytes / OriginalBytes;
            }
        }
    }
}
