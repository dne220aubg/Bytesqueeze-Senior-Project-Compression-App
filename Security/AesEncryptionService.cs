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
        private const int IterationCount = 10000;

        public byte[] Encrypt(byte[] data, string password, CancellationToken cancellationToken)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password must be provided to encrypt data.", nameof(password));
            }

            cancellationToken.ThrowIfCancellationRequested();

            byte[] salt = GenerateRandomBytes(SaltSize);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateIV();

                using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, IterationCount))
                {
                    aes.Key = keyDerivation.GetBytes(aes.KeySize / 8);
                }

                using (MemoryStream output = new MemoryStream())
                {
                    output.Write(salt, 0, salt.Length);
                    output.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    // Result buffer layout: [salt][iv][ciphertext].
                    return output.ToArray();
                }
            }
        }

        public byte[] Decrypt(byte[] cipher, string password, CancellationToken cancellationToken)
        {
            if (cipher == null)
            {
                throw new ArgumentNullException(nameof(cipher));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password must be provided to decrypt data.", nameof(password));
            }

            if (cipher.Length < SaltSize * 2)
            {
                throw new InvalidOperationException("Encrypted data is too short to contain salt and IV.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[SaltSize];

            Array.Copy(cipher, 0, salt, 0, SaltSize);
            Array.Copy(cipher, SaltSize, iv, 0, SaltSize);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.IV = iv;

                using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, IterationCount))
                {
                    aes.Key = keyDerivation.GetBytes(aes.KeySize / 8);
                }

                using (MemoryStream input = new MemoryStream(cipher, SaltSize * 2, cipher.Length - (SaltSize * 2)))
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cryptoStream.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        // Generates cryptographically strong random bytes for salts and IVs.
        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] buffer = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            return buffer;
        }
    }
}
