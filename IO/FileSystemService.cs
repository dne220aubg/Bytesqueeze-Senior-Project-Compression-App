using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.IO
{
    // Wraps basic file-system operations with validation and helper behavior used by the app.
    public sealed class FileSystemService : IFileSystemService
    {
        public async Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must be provided.", nameof(path));
            }

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[stream.Length];
                int offset = 0;
                while (offset < buffer.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    offset += read;
                }

                return buffer;
            }
        }

        public async Task WriteFileAsync(string path, byte[] data, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must be provided.", nameof(path));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        public IReadOnlyCollection<string> EnumerateFiles(string rootPath, bool includeDirectories)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path must be provided.", nameof(rootPath));
            }

            if (File.Exists(rootPath))
            {
                return new[] { Path.GetFullPath(rootPath) };
            }

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"The path '{rootPath}' could not be found.");
            }

            List<string> results = new List<string>();
            string fullRoot = Path.GetFullPath(rootPath);

            if (includeDirectories)
            {
                results.Add(fullRoot);
                results.AddRange(Directory.GetDirectories(fullRoot, "*", SearchOption.AllDirectories));
            }

            results.AddRange(Directory.GetFiles(fullRoot, "*", SearchOption.AllDirectories));

            return results;
        }

        public string GetSafeOutputPath(string inputPath, string extension)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException("Input path must be provided.", nameof(inputPath));
            }

            extension = NormalizeExtension(extension);

            string directory;
            if (File.Exists(inputPath))
            {
                directory = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Environment.CurrentDirectory;
            }
            else
            {
                directory = Path.GetFullPath(Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory);
            }

            string baseName;
            if (Directory.Exists(inputPath))
            {
                baseName = new DirectoryInfo(inputPath).Name;
            }
            else
            {
                baseName = Path.GetFileNameWithoutExtension(inputPath);
            }

            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "archive";
            }

            string candidate = Path.Combine(directory, baseName + extension);
            int counter = 1;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, $"{baseName}_{counter}{extension}");
                counter++;
            }

            return candidate;
        }

        // Ensures the extension starts with a dot and falls back to a default value.
        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return ".bin";
            }

            if (extension.StartsWith(".", StringComparison.Ordinal))
            {
                return extension;
            }

            return "." + extension;
        }
    }
}
