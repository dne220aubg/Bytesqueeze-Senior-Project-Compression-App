using System;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Tests;

namespace SeniorProjectCompressionApp
{
    public static class Verification
    {
        public static async Task Main()
        {
            await Run();
        }

        public static async Task Run()
        {
            try
            {
                // await LargeFileTest.Run();
                await EncryptedFileTest.Run();
                await EncryptionCompatibilityTest.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
        }
    }
}
