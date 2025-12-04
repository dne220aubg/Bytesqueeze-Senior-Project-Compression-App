using System;
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
    public static class EncryptedFileTest
    {
        public static async Task Run()
        {
            string inputPath = @"C:\Users\Lazaj Store\Desktop\CANADA CONFIRM LEADS (1).txt";
            string outputPath = Path.Combine(Path.GetTempPath(), "encrypted_test.spca");

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"File not found: {inputPath}");
                return;
            }

            Console.WriteLine($"Testing ENCRYPTED Compression on: {inputPath}");
            
            // Setup dependencies
            var algorithm = new DeflateAlgorithm();
            var registry = new CompressionAlgorithmRegistry(new[] { algorithm });
            var fileSystem = new FileSystemService();
            var encryption = new AesEncryptionService();
            var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryption);

            try
            {
                var summary = await orchestrator.CompressAsync(
                    inputPath,
                    "Normal",
                    "TestPassword123!", // Password
                    outputPath,
                    new Progress<double>(p => { }),
                    CancellationToken.None
                );
                
                Console.WriteLine($"\n=== ENCRYPTED RESULT ===");
                Console.WriteLine($"Success! Archive Size: {summary.ArchiveBytes:N0} bytes");
                Console.WriteLine($"Encrypted: {summary.WasEncrypted}");

                // Verify Decompression
                Console.WriteLine("\nTesting Decompression...");
                string restorePath = Path.Combine(Path.GetTempPath(), "restored_test");
                if (Directory.Exists(restorePath)) Directory.Delete(restorePath, true);

                var decompSummary = await orchestrator.DecompressAsync(
                    outputPath,
                    restorePath,
                    "TestPassword123!",
                    new Progress<double>(p => { }),
                    CancellationToken.None
                );

                Console.WriteLine($"Decompression Success! Restored: {decompSummary.RestoredBytes:N0} bytes");

                // Verify Wrong Password Handling
                Console.WriteLine("\nTesting Wrong Password...");
                try
                {
                    await orchestrator.DecompressAsync(
                        outputPath,
                        restorePath + "_wrong",
                        "WrongPassword!",
                        new Progress<double>(p => { }),
                        CancellationToken.None
                    );
                    Console.WriteLine("ERROR: Wrong password did NOT throw an exception!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Expected Error Caught: {ex.Message}");
                    if (ex.Message.Contains("Invalid password"))
                    {
                         Console.WriteLine("SUCCESS: Correctly identified invalid password.");
                    }
                    else
                    {
                         Console.WriteLine("FAILURE: Did not get expected 'Invalid password' error.");
                    }
                }
                // Verify content match
                string restoredFile = Path.Combine(restorePath, "CANADA CONFIRM LEADS (1).txt");
                if (File.Exists(restoredFile))
                {
                    long originalSize = new FileInfo(inputPath).Length;
                    long restoredSize = new FileInfo(restoredFile).Length;
                    Console.WriteLine($"Size Match: {originalSize == restoredSize} ({originalSize} vs {restoredSize})");
                }
                else
                {
                    Console.WriteLine("ERROR: Restored file not found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                // Cleanup restore dir
                string restorePath = Path.Combine(Path.GetTempPath(), "restored_test");
                if (Directory.Exists(restorePath)) Directory.Delete(restorePath, true);
            }
        }
    }
}
