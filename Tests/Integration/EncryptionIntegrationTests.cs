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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SeniorProjectCompressionApp.Tests.Integration
{
    [TestClass]
    public class EncryptionIntegrationTests
    {
        [TestMethod]
        public async Task EndToEndEncryptionTest()
        {
            Console.WriteLine("  Testing End-to-End Encryption...");

            string inputPath = Path.Combine(Path.GetTempPath(), "integration_test_data.txt");
            string outputPath = Path.Combine(Path.GetTempPath(), "encrypted_integration.spca");
            string restorePath = Path.Combine(Path.GetTempPath(), "restored_integration");
            string password = "TestPassword123!";

            // Create test data (1MB of random data)
            byte[] data = new byte[1024 * 1024];
            new Random().NextBytes(data);
            File.WriteAllBytes(inputPath, data);

            try
            {
                // Setup dependencies
                var algorithm = new DeflateAlgorithm();
                var registry = new CompressionAlgorithmRegistry(new[] { algorithm });
                var fileSystem = new FileSystemService();
                var encryption = new AesEncryptionService();
                var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryption);

                // 1. Compress
                var summary = await orchestrator.CompressAsync(
                    inputPath,
                    "Normal",
                    password,
                    outputPath,
                    new Progress<double>(p => { }),
                    CancellationToken.None
                );
                
                Assert.IsTrue(summary.WasEncrypted, "Summary should indicate encryption");
                Assert.IsTrue(File.Exists(outputPath), "Output file should exist");
                
                // Give file system a moment to settle (fixes flakiness on some systems)
                await Task.Delay(100);

                // 2. Verify Decompression
                if (Directory.Exists(restorePath)) Directory.Delete(restorePath, true);

                var decompSummary = await orchestrator.DecompressAsync(
                    outputPath,
                    restorePath,
                    password,
                    new Progress<double>(p => { }),
                    CancellationToken.None
                );

                Assert.AreEqual(data.Length, (int)decompSummary.RestoredBytes, "Restored size mismatch");

                // 3. Verify Content
                string restoredFile = Path.Combine(restorePath, "integration_test_data.txt");
                Assert.IsTrue(File.Exists(restoredFile), "Restored file not found");
                
                byte[] restoredData = File.ReadAllBytes(restoredFile);
                Assert.IsTrue(data.Length == restoredData.Length, "Data length mismatch");
                // Check first and last bytes to be sure
                Assert.AreEqual(data[0], restoredData[0], "First byte mismatch");
                Assert.AreEqual(data[^1], restoredData[^1], "Last byte mismatch");

                // 4. Verify Wrong Password Handling
                bool threw = false;
                try
                {
                    await orchestrator.DecompressAsync(
                        outputPath,
                        restorePath + "_wrong",
                        "WrongPassword!",
                        new Progress<double>(p => { }),
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Invalid password")) threw = true;
                }
                Assert.IsTrue(threw, "Should throw 'Invalid password' exception");
            }
            finally
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(outputPath)) File.Delete(outputPath);
                if (Directory.Exists(restorePath)) Directory.Delete(restorePath, true);
                if (Directory.Exists(restorePath + "_wrong")) Directory.Delete(restorePath + "_wrong", true);
            }
        }
    }
}
