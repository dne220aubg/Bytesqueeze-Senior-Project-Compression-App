using System;
using System.IO;
using System.Text;

namespace SeniorProjectCompressionApp.Compression
{
    // Encodes and decodes frequency tables into a compact binary representation for storage alongside compressed data.
    public static class FrequencyTableSerializer
    {
        // Header used to identify the compact encoding introduced to reduce metadata overhead.
        private static readonly byte[] CompactFormatHeader = new byte[] { (byte)'S', (byte)'P', (byte)'F', (byte)'1' };

        // Converts a frequency table into a Base64 string.
        public static string Encode(int[] frequencies)
        {
            if (frequencies == null)
            {
                throw new ArgumentNullException(nameof(frequencies));
            }

            if (frequencies.Length > byte.MaxValue + 1)
            {
                throw new InvalidOperationException("Frequency table length exceeds supported alphabet size.");
            }

            int nonZeroCount = 0;
            for (int i = 0; i < frequencies.Length; i++)
            {
                if (frequencies[i] != 0)
                {
                    nonZeroCount++;
                }
            }

            int legacyByteLength = frequencies.Length * sizeof(int);
            int compactByteLength = CompactFormatHeader.Length + (sizeof(int) * 2) + (nonZeroCount * (sizeof(byte) + sizeof(int)));

            // Fall back to the legacy layout when it is smaller or equal in size to avoid regressions on high-entropy data.
            if (compactByteLength >= legacyByteLength)
            {
                byte[] legacyBuffer = new byte[legacyByteLength];
                Buffer.BlockCopy(frequencies, 0, legacyBuffer, 0, legacyBuffer.Length);
                return Convert.ToBase64String(legacyBuffer);
            }

            using (MemoryStream stream = new MemoryStream(compactByteLength))
            {
                stream.Write(CompactFormatHeader, 0, CompactFormatHeader.Length);

                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(frequencies.Length);
                    writer.Write(nonZeroCount);

                    for (int i = 0; i < frequencies.Length; i++)
                    {
                        int frequency = frequencies[i];
                        if (frequency == 0)
                        {
                            continue;
                        }

                        writer.Write((byte)i);
                        writer.Write(frequency);
                    }

                    writer.Flush();
                }

                return Convert.ToBase64String(stream.ToArray());
            }
        }

        // Restores a frequency table previously created with Encode.
        public static int[] Decode(string encoded)
        {
            if (encoded == null)
            {
                throw new ArgumentNullException(nameof(encoded));
            }

            byte[] buffer = Convert.FromBase64String(encoded);

            if (IsCompactFormat(buffer))
            {
                return DecodeCompact(buffer);
            }

            int[] frequencies = new int[buffer.Length / sizeof(int)];
            Buffer.BlockCopy(buffer, 0, frequencies, 0, buffer.Length);
            return frequencies;
        }

        private static bool IsCompactFormat(byte[] buffer)
        {
            if (buffer.Length < CompactFormatHeader.Length)
            {
                return false;
            }

            for (int i = 0; i < CompactFormatHeader.Length; i++)
            {
                if (buffer[i] != CompactFormatHeader[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] DecodeCompact(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer, CompactFormatHeader.Length, buffer.Length - CompactFormatHeader.Length, writable: false))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                int length = reader.ReadInt32();
                if (length <= 0)
                {
                    throw new InvalidOperationException("Invalid frequency table length.");
                }
                if (length > byte.MaxValue + 1)
                {
                    throw new InvalidOperationException("Frequency table length exceeds supported alphabet size.");
                }

                int[] frequencies = new int[length];

                int entryCount = reader.ReadInt32();
                if (entryCount < 0 || entryCount > length)
                {
                    throw new InvalidOperationException("Invalid frequency table entry count.");
                }

                for (int i = 0; i < entryCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int frequency = reader.ReadInt32();

                    if (symbol >= length)
                    {
                        throw new InvalidOperationException("Frequency table symbol exceeds declared alphabet length.");
                    }

                    frequencies[symbol] = frequency;
                }

                return frequencies;
            }
        }
    }
}
