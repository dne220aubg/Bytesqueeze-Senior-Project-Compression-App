using System;
using System.IO;
using System.Text;

namespace SeniorProjectCompressionApp.Compression
{
    // Handles serialization of frequency tables (histogram of byte occurrences) used for Huffman tree reconstruction.
    // Uses a compact binary format to save space.
    public static class FrequencyTableSerializer
    {
        // Magic header "SPF1" to distinguish the compact format.
        private static readonly byte[] CompactFormatHeader = new byte[] { (byte)'S', (byte)'P', (byte)'F', (byte)'1' };

        // Serializes the frequency table into a Base64 string for storage in the file header.
        public static string Encode(int[] frequencies)
        {
            if (frequencies == null) throw new ArgumentNullException(nameof(frequencies));
            if (frequencies.Length > byte.MaxValue + 1) throw new InvalidOperationException("Alphabet size too large for current implementation.");

            // Count non-zero entries to estimate size of compact format (sparse representation).
            int nonZeroCount = 0;
            for (int i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] != 0) nonZeroCount++;
            }

            // Compact size: Header + Length(int) + Count(int) + [Symbol(byte) + Freq(int)] per entry.
            int compactByteLength = CompactFormatHeader.Length + (sizeof(int) * 2) + (nonZeroCount * (sizeof(byte) + sizeof(int)));

            // Write compact format: Header -> Table Length -> Entry Count -> (Symbol, Frequency) pairs.
            using (MemoryStream stream = new MemoryStream(compactByteLength))
            {
                stream.Write(CompactFormatHeader, 0, CompactFormatHeader.Length);

                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(frequencies.Length);
                    writer.Write(nonZeroCount);

                    for (int i = 0; i < frequencies.Length; i++)
                    {
                        if (frequencies[i] == 0) continue;
                        writer.Write((byte)i);
                        writer.Write(frequencies[i]);
                    }
                    writer.Flush();
                }

                return Convert.ToBase64String(stream.ToArray());
            }
        }

        // Deserializes the frequency table from a Base64 string.
        public static int[] Decode(string encoded)
        {
            if (encoded == null) throw new ArgumentNullException(nameof(encoded));

            byte[] buffer = Convert.FromBase64String(encoded);

            // Validate header.
            if (!IsCompactFormat(buffer))
            {
                throw new InvalidDataException("Invalid frequency table format. Expected compact format header.");
            }

            return DecodeCompact(buffer);
        }

        private static bool IsCompactFormat(byte[] buffer)
        {
            if (buffer.Length < CompactFormatHeader.Length) return false;

            for (int i = 0; i < CompactFormatHeader.Length; i++)
            {
                if (buffer[i] != CompactFormatHeader[i]) return false;
            }
            return true;
        }

        private static int[] DecodeCompact(byte[] buffer)
        {
            // Skip header and read the sparse table.
            using (MemoryStream stream = new MemoryStream(buffer, CompactFormatHeader.Length, buffer.Length - CompactFormatHeader.Length, writable: false))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                int length = reader.ReadInt32();
                if (length <= 0 || length > byte.MaxValue + 1) throw new InvalidOperationException("Invalid table length.");

                int[] frequencies = new int[length];
                int entryCount = reader.ReadInt32();

                for (int i = 0; i < entryCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int frequency = reader.ReadInt32();
                    frequencies[symbol] = frequency;
                }

                return frequencies;
            }
        }
    }
}
