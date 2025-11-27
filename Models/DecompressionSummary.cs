using System;

namespace SeniorProjectCompressionApp.Models
{
    // Captures high-level statistics about a completed decompression run.
    public sealed class DecompressionSummary
    {
        public DecompressionSummary(
            string destinationPath,
            string algorithmName,
            long archiveBytes,
            long restoredBytes,
            int restoredFileCount,
            bool wasEncrypted,
            long elapsedMilliseconds)
        {
            DestinationPath = destinationPath ?? throw new ArgumentNullException(nameof(destinationPath));
            AlgorithmName = algorithmName ?? string.Empty;
            ArchiveBytes = Math.Max(0, archiveBytes);
            RestoredBytes = Math.Max(0, restoredBytes);
            RestoredFileCount = Math.Max(0, restoredFileCount);
            WasEncrypted = wasEncrypted;
            ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds);
        }

        // Root directory that received the decompressed files.
        public string DestinationPath { get; }

        // Name of the algorithm used to restore the data.
        public string AlgorithmName { get; }

        // Size of the archive file that was processed.
        public long ArchiveBytes { get; }

        // Total bytes written to disk after decompression.
        public long RestoredBytes { get; }

        // Number of file entries restored (directories excluded).
        public int RestoredFileCount { get; }

        // Indicates whether the archive payload was encrypted.
        public bool WasEncrypted { get; }

        // Total time spent inside the decompression algorithm, in milliseconds.
        public long ElapsedMilliseconds { get; }

        // Expansion ratio (restored bytes divided by archive bytes); returns 1 when archive size is zero.
        public double ExpansionRatio
        {
            get
            {
                if (ArchiveBytes == 0)
                {
                    return 1.0;
                }

                return (double)RestoredBytes / ArchiveBytes;
            }
        }
    }
}
