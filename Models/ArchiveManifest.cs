using System;
using System.Collections.Generic;

namespace SeniorProjectCompressionApp.Models
{
    // Stores high-level metadata about an archived set of entries.
    public sealed class ArchiveManifest
    {
        public ArchiveManifest(string rootName, IReadOnlyCollection<ArchiveEntry> entries, bool isDirectory)
        {
            RootName = rootName;
            Entries = entries;
            IsDirectory = isDirectory;
            CreatedUtc = DateTime.UtcNow;
        }

        // Root name chosen for restoring the archive.
        public string RootName { get; }

        // Immutable collection of stored entries.
        public IReadOnlyCollection<ArchiveEntry> Entries { get; }

        // True if the original payload represented a directory.
        public bool IsDirectory { get; }

        // Timestamp indicating when the manifest was created.
        public DateTime CreatedUtc { get; }
    }
}
