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
    public static class LargeFileTest
    {
        public static async Task Run()
        {
            string inputPath = @"C:\Users\Lazaj Store\Downloads\Regjistri i Gjendjes Civile (NeÌˆntor 2008) ver.1.4.mdb";
            string outputPath = Path.Combine(Path.GetTempPath(), "large_test.spca");

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"File not found: {inputPath}");
                // Try the other file if this one is missing
                inputPath = @"C:\Users\Lazaj Store\Desktop\CANADA CONFIRM LEADS (1).txt";
                if (!File.Exists(inputPath)) return;
            }

            Console.WriteLine($"Testing PARALLEL Compression on: {inputPath}");
            long fileSize = new FileInfo(inputPath).Length;
            Console.WriteLine($"Size: {fileSize:N0} bytes ({(double)fileSize / 1024 / 1024:F2} MB)");
            
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
                    inputPath,
                    "Normal",
                    null, // No password
                    outputPath,
                    new Progress<double>(p => { 
                        if (p * 100 % 10 == 0) Console.Write("."); 
                    }),
                    CancellationToken.None
                );
                
                sw.Stop();
                
                Console.WriteLine($"\n=== LARGE FILE RESULTS ===");
                Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} s)");
                Console.WriteLine($"Throughput: {(double)fileSize / 1024 / 1024 / sw.Elapsed.TotalSeconds:F2} MB/s");
                Console.WriteLine($"Archive Size: {summary.ArchiveBytes:N0} bytes");
                Console.WriteLine($"Ratio: {summary.CompressionRatio:P2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }
    }
}

