using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace SeniorProjectCompressionApp.Security
{
    // Uses AES with PBKDF2-derived keys to encrypt and decrypt archive payloads.
    public sealed class AesEncryptionService : IEncryptionService
    {
        private const int SaltSize = 16;
        private const int DefaultIterationCount = 100000; // Adjusted for performance (approx 100ms)
        private static readonly HashAlgorithmName DefaultHashAlgorithm = HashAlgorithmName.SHA256;

        public Stream EncryptStream(Stream output, string password, CancellationToken cancellationToken)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password required.", nameof(password));

            byte[] salt = GenerateRandomBytes(SaltSize);
            
            // Write Salt immediately
            output.Write(salt, 0, salt.Length);

            using (var derive = new Rfc2898DeriveBytes(password, salt, DefaultIterationCount, DefaultHashAlgorithm))
            {
                byte[] key = derive.GetBytes(32); // AES-256

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();

                    // Write IV
                    output.Write(aes.IV, 0, aes.IV.Length);

                    // Return CryptoStream (caller must dispose/flush)
                    return new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
                }
            }
        }

        public Stream DecryptStream(Stream input, string password, int iterations, string hashAlgorithmName, CancellationToken cancellationToken)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password required.", nameof(password));

            byte[] salt = new byte[SaltSize];
            int read = input.Read(salt, 0, SaltSize);
            if (read != SaltSize) throw new EndOfStreamException("Stream too short for salt.");

            byte[] iv = new byte[16]; // AES block size is 16 bytes
            read = input.Read(iv, 0, 16);
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
