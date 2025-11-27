using System;
using System.Collections.Generic;

namespace SeniorProjectCompressionApp.Models
{
    // Describes the information required to reverse a compression operation.
    public sealed class CompressionMetadata
    {
        public CompressionMetadata(string algorithmName, int originalSize, IDictionary<string, string>? attributes = null)
        {
            AlgorithmName = algorithmName;
            OriginalSize = originalSize;
            Attributes = attributes is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);
        }

        // Algorithm that produced the compressed payload.
        public string AlgorithmName { get; }

        // Size of the input data before compression.
        public int OriginalSize { get; }

        // Algorithm-specific attributes used during decompression.
        public IDictionary<string, string> Attributes { get; }
    }
}
