using System.Threading;

namespace SeniorProjectCompressionApp.Security
{
    // Provides symmetric encryption and decryption services for archive payloads.
    public interface IEncryptionService
    {
        // Encrypts data using the provided password.
        byte[] Encrypt(byte[] data, string password, CancellationToken cancellationToken);

        // Decrypts data that was previously encrypted with the same password.
        byte[] Decrypt(byte[] cipher, string password, CancellationToken cancellationToken);
    }
}
