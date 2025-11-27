using System;

namespace SeniorProjectCompressionApp.Models
{
    // Represents a single file system entry captured within an archive.
    public sealed class ArchiveEntry
    {
        public ArchiveEntry(string relativePath, bool isDirectory, long originalLength, bool storedAsRaw = false)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Relative path must be provided.", nameof(relativePath));
            }

            RelativePath = relativePath.Replace('\\', '/');
            IsDirectory = isDirectory;

            if (isDirectory && storedAsRaw)
            {
                throw new ArgumentException("Directories cannot be marked as raw entries.", nameof(storedAsRaw));
            }

            StoredAsRaw = storedAsRaw;
            OriginalLength = originalLength;
        }

        // Relative path (using forward slashes) inside the archive.
        public string RelativePath { get; }

        // True when the entry represents a directory.
        public bool IsDirectory { get; }

        // Indicates whether the entry's data was stored without compression.
        public bool StoredAsRaw { get; }

        // Original length of the file prior to compression.
        public long OriginalLength { get; }
    }
}
