using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.Compression.Algorithms;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Models;
using SeniorProjectCompressionApp.Security;
using SeniorProjectCompressionApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SeniorProjectCompressionApp.Tests.Integration
{
    [TestClass]
    // Integration test: compress a large real-world file
    public class LargeFileTests
    {
        [TestMethod]
        public async Task ParallelCompressionTest()
        {
            string filePath = @"C:\Users\Lazaj Store\Desktop\BigDatabase.mdb";
            bool createdTemp = false;

            Console.WriteLine($"Testing PARALLEL Compression on: {filePath}");
            FileInfo info = new FileInfo(filePath);
            Console.WriteLine($"Size: {info.Length:N0} bytes ({info.Length / (1024.0 * 1024.0):F2} MB)");

            string outputPath = Path.Combine(Path.GetTempPath(), "largefile_test.spca");

            try
            {
                // Setup dependencies
                var algorithm = new DeflateAlgorithm();
                var registry = new CompressionAlgorithmRegistry(new[] { algorithm });
                var fileSystem = new FileSystemService();
                var encryption = new AesEncryptionService();
                var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryption);

                Console.WriteLine("Starting compression...");

                var sw = Stopwatch.StartNew();

                var summary = await orchestrator.CompressAsync(
                    filePath,
                    "Normal",
                    null, // No password
                    outputPath,
                    new Progress<double>(p => { }),
                    CancellationToken.None
                );

                sw.Stop();

                Console.WriteLine("\n=== LARGE FILE RESULTS ===");
                Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} s)");
                double mb = summary.OriginalBytes / (1024.0 * 1024.0);
                double seconds = sw.Elapsed.TotalSeconds;
                Console.WriteLine($"Throughput: {mb / seconds:F2} MB/s");
                Console.WriteLine($"Archive Size: {summary.ArchiveBytes:N0} bytes");
                Console.WriteLine($"Ratio: {(double)summary.ArchiveBytes / summary.OriginalBytes:P2}");

                Assert.IsTrue(File.Exists(outputPath), "Output file should exist");
                Assert.IsTrue(summary.ArchiveBytes > 0, "Archive size should be > 0");
                Assert.IsTrue(summary.CompressedFileCount == 1, "Should have compressed 1 file");
            }
            finally
            {
                // Cleanup
                if (File.Exists(outputPath)) File.Delete(outputPath);
                if (createdTemp && File.Exists(filePath)) File.Delete(filePath);
            }
        }
    }
}
