using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.Services;
using SeniorProjectCompressionApp.Compression.Algorithms;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Security;

namespace SeniorProjectCompressionApp.Tests
{
    public class FullOrchestratorTest
    {
        public static void Run()
        {
            string filePath = @"C:\Users\Lazaj Store\Desktop\CANADA CONFIRM LEADS (1).txt";
            string outputPath = @"C:\Users\Lazaj Store\Desktop\SeniorProjectCompressionApp\Tests\test_archive.spca";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: File not found: {filePath}");
                return;
            }
            
            Console.WriteLine($"Testing FULL APP FLOW (like your UI does):");
            Console.WriteLine($"Input: {filePath}");
            FileInfo info = new FileInfo(filePath);
            Console.WriteLine($"Size: {info.Length:N0} bytes ({info.Length / (1024.0 * 1024.0):F2} MB)\n");
            
            // Create the EXACT same setup as your Form1
            ICompressionAlgorithm[] algorithms = new ICompressionAlgorithm[]
            {
                new DeflateAlgorithm()
            };
            
            var registry = new CompressionAlgorithmRegistry(algorithms);
            var fileSystem = new FileSystemService();
            var encryptionService = new AesEncryptionService();

            var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryptionService);
            
            Console.WriteLine("Compressing via orchestrator (NO encryption, NO password)...");
            var sw = Stopwatch.StartNew();
            
            var summary = orchestrator.CompressAsync(
                filePath,
                "Normal",
                password: null,  // No encryption
                outputPath,
                progress: null,
                CancellationToken.None).Result;
            
            sw.Stop();
            
            Console.WriteLine($"\n=== FULL ORCHESTRATOR RESULTS ===");
            Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2}s)");
            Console.WriteLine($"Algorithm-Only Time: {summary.ElapsedMilliseconds} ms");  
            Console.WriteLine($"Overhead: {sw.ElapsedMilliseconds - summary.ElapsedMilliseconds} ms");
            Console.WriteLine($"Compressed Size: {summary.ArchiveBytes:N0} bytes");
            Console.WriteLine($"Ratio: {summary.CompressionRatio:P2}");
            
            if (sw.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine($"\nâš ï¸  TOO SLOW: {sw.ElapsedMilliseconds}ms > 1000ms");
                Console.WriteLine($"Algorithm time is only {summary.ElapsedMilliseconds}ms");
                Console.WriteLine($"Extra overhead: {sw.ElapsedMilliseconds - summary.ElapsedMilliseconds}ms");
            }
            else
            {
                Console.WriteLine("\nâœ…  GOAL MET!");
            }
            
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}

