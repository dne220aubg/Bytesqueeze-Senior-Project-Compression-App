using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.IO
{
    // Abstraction for the file-system interactions required during compression workflows.
    public interface IFileSystemService
    {
        // Reads the entire contents of a file as a byte array.
        Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken);

        // Writes the provided data to disk, creating intermediate directories when needed.
        Task WriteFileAsync(string path, byte[] data, CancellationToken cancellationToken);

        // Enumerates files (and optionally directories) rooted at the supplied path.
        IReadOnlyCollection<string> EnumerateFiles(string rootPath, bool includeDirectories);

        // Suggests a file name for produced archives that avoids overwriting existing files.
        string GetSafeOutputPath(string inputPath, string extension);
    }
}
