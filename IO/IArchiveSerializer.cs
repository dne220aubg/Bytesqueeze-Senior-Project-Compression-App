using SeniorProjectCompressionApp.Models;

namespace SeniorProjectCompressionApp.IO
{
    // Serializes and deserializes archive packages to a binary representation.
    public interface IArchiveSerializer
    {
        // Converts an archive package to a byte array ready for persistence.
        byte[] Serialize(ArchivePackage package);

        // Rehydrates an archive package from a serialized byte array.
        ArchivePackage Deserialize(byte[] data);
    }
}
