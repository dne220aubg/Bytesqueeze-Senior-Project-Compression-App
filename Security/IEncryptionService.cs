using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.Security
{
    // Provides symmetric encryption and decryption services for archive payloads.
    public interface IEncryptionService
    {
        // Encrypts data to the output stream using the provided password.
        Task<Stream> EncryptStreamAsync(Stream output, string password, CancellationToken cancellationToken);

        // Decrypts data from the input stream using the provided password and KDF parameters.
        Task<Stream> DecryptStreamAsync(Stream input, string password, int iterations, string hashAlgorithmName, CancellationToken cancellationToken);
    }
}
