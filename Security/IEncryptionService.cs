using System.IO;
using System.Threading;

namespace SeniorProjectCompressionApp.Security
{
    // Provides symmetric encryption and decryption services for archive payloads.
    public interface IEncryptionService
    {
        // Encrypts data to the output stream using the provided password.
        Stream EncryptStream(Stream output, string password, CancellationToken cancellationToken);

        // Decrypts data from the input stream using the provided password and KDF parameters.
        Stream DecryptStream(Stream input, string password, int iterations, string hashAlgorithmName, CancellationToken cancellationToken);
    }
}
