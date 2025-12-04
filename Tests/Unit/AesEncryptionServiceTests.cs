using System;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using SeniorProjectCompressionApp.Security;

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SeniorProjectCompressionApp.Tests.Unit
{
    [TestClass]
    public class AesEncryptionServiceTests
    {
        [TestMethod]
        public async Task LifecycleTest()
        {
            Console.WriteLine("  Testing Lifecycle...");
            
            var service = new AesEncryptionService();
            string password = "TestPassword";
            byte[] data = new byte[1024];
            new Random().NextBytes(data);

            // Test Encryption Lifecycle
            using (var ms = new MemoryStream())
            {
                Stream cryptoStream = await service.EncryptStreamAsync(ms, password, CancellationToken.None);
                
                // Assert that we can write without throwing ObjectDisposedException
                await cryptoStream.WriteAsync(data, 0, data.Length);
                await cryptoStream.FlushAsync();
                cryptoStream.Dispose();
            }

            // Test Decryption Lifecycle
            byte[] encryptedData;
            using (var ms = new MemoryStream())
            {
                var s = await service.EncryptStreamAsync(ms, password, CancellationToken.None);
                await s.WriteAsync(data, 0, data.Length);
                s.Close();
                encryptedData = ms.ToArray();
            }

            using (var ms = new MemoryStream(encryptedData))
            {
                Stream cryptoStream = await service.DecryptStreamAsync(ms, password, 100000, "SHA256", CancellationToken.None);

                byte[] buffer = new byte[1024];
                int read = await cryptoStream.ReadAsync(buffer, 0, buffer.Length);
                
                Assert.IsTrue(read > 0, "Should read decrypted data");
                cryptoStream.Dispose();
            }
        }
    }
}
