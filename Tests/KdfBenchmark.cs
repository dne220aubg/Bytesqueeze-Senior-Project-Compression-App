using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.Tests
{
    public static class KdfBenchmark
    {
        public static Task Run()
        {
            Console.WriteLine("\n=== KDF Performance Benchmark ===");
            
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            string password = "BenchmarkPassword123!";

            // Warmup
            Derive(password, salt, 1000);

            // Test 10,000
            var sw = Stopwatch.StartNew();
            Derive(password, salt, 10000);
            sw.Stop();
            Console.WriteLine($"10,000 iterations: {sw.ElapsedMilliseconds} ms");

            // Test 600,000
            sw.Restart();
            Derive(password, salt, 600000);
            sw.Stop();
            Console.WriteLine($"600,000 iterations: {sw.ElapsedMilliseconds} ms");
            
            return Task.CompletedTask;
        }

        private static void Derive(string password, byte[] salt, int iterations)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                _ = derive.GetBytes(32);
            }
        }
    }
}
