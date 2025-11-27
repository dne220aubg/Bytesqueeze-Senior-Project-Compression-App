using System.Collections.Generic;

using SeniorProjectCompressionApp.Compression;

namespace SeniorProjectCompressionApp.Services
{
    // Provides lookup services for registered compression algorithms.
    public interface ICompressionAlgorithmRegistry
    {
        // Enumerates the algorithms available to the application.
        IReadOnlyCollection<ICompressionAlgorithm> GetAlgorithms();

        // Retrieves a specific algorithm by name, if it has been registered.
        ICompressionAlgorithm? GetAlgorithm(string name);
    }
}
