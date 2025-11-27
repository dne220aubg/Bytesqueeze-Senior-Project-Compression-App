using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.IO
{
    // Encodes archive packages into a compact binary format and restores them on demand.
    public sealed class BinaryArchiveSerializer : IArchiveSerializer
    {
        private static readonly byte[] MagicHeader = Encoding.ASCII.GetBytes("SPCA");
        private const byte CurrentVersion = 2;

        public byte[] Serialize(ArchivePackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                stream.Write(MagicHeader, 0, MagicHeader.Length);
                writer.Write(CurrentVersion);
                writer.Write(package.IsEncrypted);

                WriteManifest(writer, package.Manifest, CurrentVersion);
                WriteMetadata(writer, package.CompressionResult.Metadata);

                byte[] compressedData = package.CompressionResult.CompressedData ?? Array.Empty<byte>();
                writer.Write(compressedData.Length);
                stream.Write(compressedData, 0, compressedData.Length);

                writer.Flush();
                return stream.ToArray();
            }
        }

        public ArchivePackage Deserialize(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (MemoryStream stream = new MemoryStream(data, writable: false))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                ValidateHeader(stream);

                byte version = reader.ReadByte();
                if (version < 1 || version > CurrentVersion)
                {
                    throw new InvalidOperationException($"Unsupported archive version: {version}.");
                }

                bool isEncrypted = reader.ReadBoolean();
                ArchiveManifest manifest = ReadManifest(reader, version);
                CompressionMetadata metadata = ReadMetadata(reader);

                int dataLength = reader.ReadInt32();
                byte[] compressedData = reader.ReadBytes(dataLength);

                CompressionResult compressionResult = new CompressionResult(metadata, compressedData);
                return new ArchivePackage(manifest, compressionResult, isEncrypted);
            }
        }

        // Writes the manifest portion of the archive.
        private static void WriteManifest(BinaryWriter writer, ArchiveManifest manifest, byte version)
        {
            writer.Write(manifest.RootName ?? string.Empty);
            writer.Write(manifest.IsDirectory);
            writer.Write(manifest.Entries.Count);

            foreach (ArchiveEntry entry in manifest.Entries)
            {
                writer.Write(entry.RelativePath);
                writer.Write(entry.IsDirectory);
                if (version >= 2)
                {
                    writer.Write(entry.StoredAsRaw);
                }
                writer.Write(entry.OriginalLength);
            }
        }

        // Reads the manifest that enumerates all archived entries.
        private static ArchiveManifest ReadManifest(BinaryReader reader, byte version)
        {
            string rootName = reader.ReadString();
            bool isDirectory = reader.ReadBoolean();
            int entryCount = reader.ReadInt32();
            List<ArchiveEntry> entries = new List<ArchiveEntry>(entryCount);

            for (int i = 0; i < entryCount; i++)
            {
                string relativePath = reader.ReadString();
                bool entryIsDirectory = reader.ReadBoolean();
                bool storedAsRaw = false;
                if (version >= 2)
                {
                    storedAsRaw = reader.ReadBoolean();
                }
                long originalLength = reader.ReadInt64();
                entries.Add(new ArchiveEntry(relativePath, entryIsDirectory, originalLength, storedAsRaw));
            }

            return new ArchiveManifest(rootName, entries, isDirectory);
        }

        // Writes compression metadata to the stream.
        private static void WriteMetadata(BinaryWriter writer, CompressionMetadata metadata)
        {
            writer.Write(metadata.AlgorithmName ?? string.Empty);
            writer.Write(metadata.OriginalSize);
            writer.Write(metadata.Attributes.Count);

            foreach (KeyValuePair<string, string> pair in metadata.Attributes)
            {
                writer.Write(pair.Key ?? string.Empty);
                writer.Write(pair.Value ?? string.Empty);
            }
        }

        // Reads compression metadata from the stream.
        private static CompressionMetadata ReadMetadata(BinaryReader reader)
        {
            string algorithmName = reader.ReadString();
            int originalSize = reader.ReadInt32();
            int attributeCount = reader.ReadInt32();

            Dictionary<string, string> attributes = new Dictionary<string, string>(attributeCount, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < attributeCount; i++)
            {
                string key = reader.ReadString();
                string value = reader.ReadString();
                attributes[key] = value;
            }

            return new CompressionMetadata(algorithmName, originalSize, attributes);
        }

        // Ensures the incoming data starts with the expected magic header.
        private static void ValidateHeader(Stream stream)
        {
            byte[] header = new byte[MagicHeader.Length];
            int read = stream.Read(header, 0, header.Length);

            if (read != MagicHeader.Length || !AreEqual(header, MagicHeader))
            {
                throw new InvalidOperationException("Data does not appear to be a valid Senior Project archive.");
            }
        }

        // Performs constant-time equality comparison between two byte arrays.
        private static bool AreEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
