using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using SeniorProjectCompressionApp.Security;

namespace SeniorProjectCompressionApp.Tests
{
    public static class LifecycleTest
    {
        public static void Run()
        {
            Console.WriteLine("\n=== AES Lifecycle Test ===");
            
            var service = new AesEncryptionService();
            string password = "TestPassword";
            byte[] data = new byte[1024];
            new Random().NextBytes(data);

            // Test Encryption Lifecycle
            Console.WriteLine("Testing Encryption Stream...");
            using (var ms = new MemoryStream())
            {
                // This method disposes Aes internally before returning
                Stream cryptoStream = service.EncryptStream(ms, password, CancellationToken.None);
                
                // Try to write to it. If Aes disposal killed the transform, this might fail.
                try
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.Flush();
                    Console.WriteLine("Write successful (Transform survived Aes disposal).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Write FAILED: {ex.GetType().Name} - {ex.Message}");
                }
                finally
                {
                    cryptoStream.Dispose();
                }
            }

            // Test Decryption Lifecycle
            Console.WriteLine("Testing Decryption Stream...");
            // First create valid encrypted data
            byte[] encryptedData;
            using (var ms = new MemoryStream())
            {
                // We know this works from previous tests, or we assume it does for setup
                var s = service.EncryptStream(ms, password, CancellationToken.None);
                s.Write(data, 0, data.Length);
                s.Close();
                encryptedData = ms.ToArray();
            }

            using (var ms = new MemoryStream(encryptedData))
            {
                // This method disposes Aes internally before returning
                Stream cryptoStream = service.DecryptStream(ms, password, 100000, "SHA256", CancellationToken.None);

                try
                {
                    byte[] buffer = new byte[1024];
                    int read = cryptoStream.Read(buffer, 0, buffer.Length);
                    Console.WriteLine($"Read successful: {read} bytes (Transform survived Aes disposal).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Read FAILED: {ex.GetType().Name} - {ex.Message}");
                }
                finally
                {
                    cryptoStream.Dispose();
                }
            }
        }
    }
}
