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

namespace SeniorProjectCompressionApp.Tests
{
    public static class FolderCompressionTest
    {
        public static async Task Run()
        {
            string folderPath = @"C:\Users\Lazaj Store\Desktop\SeniorProjectCompressionApp\Tests\TestData";
            string outputPath = Path.Combine(Path.GetTempPath(), "folder_test.spca");

            // Setup dependencies
            var algorithm = new DeflateAlgorithm();
            var registry = new CompressionAlgorithmRegistry(new[] { algorithm });
            
            var fileSystem = new FileSystemService();
            var encryption = new AesEncryptionService();
            
            var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryption);

            // Run Compression
            Console.WriteLine("Starting compression...");
            Stopwatch sw = Stopwatch.StartNew();
            
            try
            {
                var summary = await orchestrator.CompressAsync(
                    folderPath,
                    "Normal",
                    null, // No password
                    outputPath,
                    new Progress<double>(p => { /* ignore */ }),
                    CancellationToken.None
                );
                
                sw.Stop();
                
                Console.WriteLine($"\n=== FOLDER COMPRESSION RESULTS ===");
                Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} s)");
                Console.WriteLine($"Files Compressed: {summary.CompressedFileCount}");
                Console.WriteLine($"Original Size: {summary.OriginalBytes:N0} bytes");
                Console.WriteLine($"Archive Size: {summary.ArchiveBytes:N0} bytes");
                Console.WriteLine($"Ratio: {summary.CompressionRatio:P2}");
                
                if (sw.ElapsedMilliseconds < 20000)
                {
                    Console.WriteLine("SUCCESS: Faster than 20s!");
                }
                else
                {
                    Console.WriteLine("FAIL: Slower than 20s.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}
