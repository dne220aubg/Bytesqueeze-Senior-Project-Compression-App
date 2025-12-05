using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.Compression.Algorithms;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Security;
using SeniorProjectCompressionApp.Services;

namespace SeniorProjectCompressionApp.Tests.Integration
{
    [TestClass]
    public class OrchestratorTests
    {
        [TestMethod]
        public async Task FullAppFlowTest()
        {
            // Using a real file
            string filePath = @"C:\Users\Lazaj Store\Desktop\CANADA CONFIRM LEADS (1).txt";
            bool createdTemp = false;

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Note: Large test file not found at {filePath}. Creating a smaller temporary file for testing.");
                filePath = Path.Combine(Path.GetTempPath(), "orchestrator_test_dummy.txt");
                byte[] data = new byte[10 * 1024 * 1024]; // 10MB
                new Random().NextBytes(data);
                File.WriteAllBytes(filePath, data);
                createdTemp = true;
            }
            
            Console.WriteLine($"Testing FULL APP FLOW:");
            Console.WriteLine($"Input: {filePath}");
            FileInfo info = new FileInfo(filePath);
            Console.WriteLine($"Size: {info.Length:N0} bytes ({info.Length / (1024.0 * 1024.0):F2} MB)\n");

            string outputPath = Path.Combine(Path.GetTempPath(), "orchestrator_test.spca");
            
            try
            {
                // Setup dependencies
                var algorithm = new DeflateAlgorithm();
                var registry = new CompressionAlgorithmRegistry(new[] { algorithm });
                var fileSystem = new FileSystemService();
                var encryption = new AesEncryptionService();
                var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryption);

                Console.WriteLine("Compressing via orchestrator (NO encryption, NO password)...");
                
                // Measure Time
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

                Console.WriteLine("\n=== FULL ORCHESTRATOR RESULTS ===");
                Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2}s)");
                Console.WriteLine($"Algorithm-Only Time: {summary.ElapsedMilliseconds} ms");
                Console.WriteLine($"Overhead: {sw.ElapsedMilliseconds - summary.ElapsedMilliseconds} ms");
                Console.WriteLine($"Compressed Size: {summary.ArchiveBytes:N0} bytes");
                Console.WriteLine($"Ratio: {(double)summary.ArchiveBytes / summary.OriginalBytes:P2}");
                
                Assert.IsTrue(File.Exists(outputPath), "Output file should exist");
                Assert.IsTrue(summary.ArchiveBytes > 0, "Archive size should be > 0");

                if (sw.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"\n  TOO SLOW: {sw.ElapsedMilliseconds}ms > 1000ms");
                }
                else
                {
                    Console.WriteLine("\n  GOAL MET!");
                }
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
