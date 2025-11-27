using System;
using System.IO;

namespace SeniorProjectCompressionApp.IO
{
    // Helper methods for manipulating paths in a platform-agnostic manner.
    public static class PathUtilities
    {
        // Calculates the relative path from rootPath to fullPath.
        public static string GetRelativePath(string rootPath, string fullPath)
        {
            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            Uri rootUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(rootPath)));
            Uri fullUri = new Uri(Path.GetFullPath(fullPath));

            Uri relativeUri = rootUri.MakeRelativeUri(fullUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        // Normalizes directory paths to ensure URI calculations behave correctly.
        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
    }
}
