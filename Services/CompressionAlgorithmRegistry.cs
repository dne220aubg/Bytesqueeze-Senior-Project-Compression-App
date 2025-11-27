using System;
using System.Collections.Generic;
using System.Linq;

using SeniorProjectCompressionApp.Compression;

namespace SeniorProjectCompressionApp.Services
{
    // Stores compression algorithms in memory for quick lookup.
    public sealed class CompressionAlgorithmRegistry : ICompressionAlgorithmRegistry
    {
        private readonly Dictionary<string, ICompressionAlgorithm> _algorithms;

        public CompressionAlgorithmRegistry(IEnumerable<ICompressionAlgorithm> algorithms)
        {
            if (algorithms == null)
            {
                throw new ArgumentNullException(nameof(algorithms));
            }

            _algorithms = algorithms.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<ICompressionAlgorithm> GetAlgorithms()
        {
            return _algorithms.Values.ToList().AsReadOnly();
        }

        public ICompressionAlgorithm? GetAlgorithm(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            _algorithms.TryGetValue(name, out ICompressionAlgorithm? algorithm);
            return algorithm;
        }
    }
}
