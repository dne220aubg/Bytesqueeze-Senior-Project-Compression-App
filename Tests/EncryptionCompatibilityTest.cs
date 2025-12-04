using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.Compression.Algorithms;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Security;
using SeniorProjectCompressionApp.Services;

namespace SeniorProjectCompressionApp.Tests
{
    public static class EncryptionCompatibilityTest
    {
        public static async Task Run()
        {
            Console.WriteLine("Running Encryption Compatibility Test...");

            string inputPath = Path.Combine(Path.GetTempPath(), "encryption_test_input.txt");
            string outputPath = Path.Combine(Path.GetTempPath(), "encryption_test_v3.spca");
            string restorePath = Path.Combine(Path.GetTempPath(), "encryption_test_restore");
            string password = "TestPassword123!";

            // 1. Create Test Data
            File.WriteAllText(inputPath, "This is some sensitive data that needs strong encryption.");

            try
            {
                // Setup dependencies
                var algorithm = new DeflateAlgorithm();
                var registry = new CompressionAlgorithmRegistry(new[] { algorithm });
                var fileSystem = new FileSystemService();
                var encryption = new AesEncryptionService(); // New implementation
                var orchestrator = new CompressionOrchestrator(registry, fileSystem, encryption);

                // 2. Compress (Create v3 Archive)
                Console.WriteLine("Compressing (Creating v3 Archive)...");
                await orchestrator.CompressAsync(
                    inputPath,
                    "Normal",
                    password,
                    outputPath,
                    null,
                    CancellationToken.None
                );

                // 3. Verify Header (Check Version and Parameters)
                Console.WriteLine("Verifying Header...");
                using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    // Raw Header "SPCR1"
                    byte[] header = reader.ReadBytes(5);
                    if (Encoding.ASCII.GetString(header) != "SPCR1") throw new Exception("Invalid Raw Header");

                    // Version
                    int version = reader.ReadInt32();
                    Console.WriteLine($"Version: {version}");
                    if (version != 3) throw new Exception($"Expected Version 3, got {version}");

                    bool isEncrypted = reader.ReadBoolean();
                    if (!isEncrypted) throw new Exception("Expected Encrypted flag to be true");

                    string algoName = reader.ReadString();
                    string rootName = reader.ReadString();
                    bool isDir = reader.ReadBoolean();

                    // KDF Parameters
                    int iterations = reader.ReadInt32();
                    string hashAlgo = reader.ReadString();

                    Console.WriteLine($"Iterations: {iterations}");
                    Console.WriteLine($"Hash Algorithm: {hashAlgo}");

                    if (iterations != 100000) throw new Exception($"Expected 100000 iterations, got {iterations}");
                    if (hashAlgo != "SHA256") throw new Exception($"Expected SHA256, got {hashAlgo}");
                }

                // 4. Decompress (Verify Decryption)
                Console.WriteLine("Decompressing...");
                if (Directory.Exists(restorePath)) Directory.Delete(restorePath, true);
                
                await orchestrator.DecompressAsync(
                    outputPath,
                    restorePath,
                    password,
                    null,
                    CancellationToken.None
                );

                string restoredFile = Path.Combine(restorePath, "encryption_test_input.txt");
                if (!File.Exists(restoredFile)) throw new Exception("Restored file not found");
                
                string restoredContent = File.ReadAllText(restoredFile);
                if (restoredContent != "This is some sensitive data that needs strong encryption.")
                    throw new Exception("Content mismatch");

                Console.WriteLine("SUCCESS: Encryption Standardization Verified!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILURE: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw; // Rethrow to fail the build/test run if we were running via a runner
            }
            finally
            {
                // Cleanup
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(outputPath)) File.Delete(outputPath);
                if (Directory.Exists(restorePath)) Directory.Delete(restorePath, true);
            }
        }
    }
}
