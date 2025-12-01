namespace SeniorProjectCompressionApp.Compression
{
    // Represents a compression algorithm that can transform raw data into a compact form and reverse the process.
    public interface ICompressionAlgorithm
    {
        // Display name shown in the UI.
        string Name { get; }
    }
}
