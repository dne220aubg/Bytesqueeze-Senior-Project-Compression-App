using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.Security
{
    // Encryption layer for archives. Uses AES with PBKDF2-derived keys to encrypt and decrypt archive payloads.
    public sealed class AesEncryptionService : IEncryptionService
    {
        private const int SaltSize = 16;
        private const int DefaultIterationCount = 100000; // Adjusted for performance (approx 100ms)
        private static readonly HashAlgorithmName DefaultHashAlgorithm = HashAlgorithmName.SHA256;

        public async Task<Stream> EncryptStreamAsync(Stream output, string password, CancellationToken cancellationToken)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password required.", nameof(password));

            byte[] salt = GenerateRandomBytes(SaltSize);
            
            // Write Salt immediately (Async)
            await output.WriteAsync(salt, 0, salt.Length, cancellationToken).ConfigureAwait(false);

            using (var derive = new Rfc2898DeriveBytes(password, salt, DefaultIterationCount, DefaultHashAlgorithm))
            {
                byte[] key = derive.GetBytes(32); // AES-256

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();

                    // Write IV (Async)
                    await output.WriteAsync(aes.IV, 0, aes.IV.Length, cancellationToken).ConfigureAwait(false);

                    // Return CryptoStream (caller must dispose/flush)
                    return new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
                }
            }
        }

        public async Task<Stream> DecryptStreamAsync(Stream input, string password, int iterations, string hashAlgorithmName, CancellationToken cancellationToken)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password required.", nameof(password));

            byte[] salt = new byte[SaltSize];
            int read = await ReadFullAsync(input, salt, cancellationToken).ConfigureAwait(false);
            if (read != SaltSize) throw new EndOfStreamException("Stream too short for salt.");

            byte[] iv = new byte[16];
            read = await ReadFullAsync(input, iv, cancellationToken).ConfigureAwait(false);
            if (read != 16) throw new EndOfStreamException("Stream too short for IV.");

            HashAlgorithmName hashAlgo = new HashAlgorithmName(hashAlgorithmName);

            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, hashAlgo))
            {
                byte[] key = derive.GetBytes(32);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    return new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true);
                }
            }
        }

        private static async Task<int> ReadFullAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, token).ConfigureAwait(false);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] buffer = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return buffer;
        }
    }
}
