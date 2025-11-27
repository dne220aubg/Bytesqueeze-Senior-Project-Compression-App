using System;
using System.Threading;
using System.Threading.Tasks;

using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.Services
{
    // Coordinates compression and decompression operations across the file system, algorithms, and persistence layers.
    public interface ICompressionOrchestrator
    {
        // Compresses files or directories and writes the resulting archive to disk.
        Task<CompressionSummary> CompressAsync(
            string inputPath,
            string algorithmName,
            string? password,
            string? outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken);

        // Restores an archive produced by CompressAsync to the destination directory.
        Task<DecompressionSummary> DecompressAsync(
            string archivePath,
            string destinationDirectory,
            string? password,
            IProgress<double>? progress,
            CancellationToken cancellationToken);
    }
}
