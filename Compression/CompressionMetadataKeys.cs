namespace SeniorProjectCompressionApp.Compression
{
    // Defines metadata keys persisted with compressed payloads.
    public static class CompressionMetadataKeys
    {
        // Key used to store the serialized frequency table.
        public const string FrequencyTable = "FrequencyTable";

        // Key used to store the number of significant bits in the compressed stream.
        public const string BitLength = "BitLength";
    }
}
