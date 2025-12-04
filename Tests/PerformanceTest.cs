using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Compression.Algorithms;

namespace SeniorProjectCompressionApp.Tests
{
    public class PerformanceTest
    {
        public static void Run()
        {
            Console.WriteLine("Generating test data (13MB)...");
            byte[] data = GenerateTextData(13 * 1024 * 1024); // 13MB
            
            Console.WriteLine($"Data size: {data.Length:N0} bytes");
            
            var compressor = new DeflateAlgorithm();
            
            // Warmup
            Console.WriteLine("Warming up...");
            Compress(compressor, new byte[1024 * 1024]);
            
            Console.WriteLine("Starting benchmark...");
            var sw = Stopwatch.StartNew();
            
            byte[] compressed = Compress(compressor, data);
            
            sw.Stop();
            
            double ratio = (double)compressed.Length / data.Length * 100.0;
            Console.WriteLine($"\nResults:");
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Compressed Size: {compressed.Length:N0} bytes");
            Console.WriteLine($"Ratio: {ratio:F2}%");
            
            if (sw.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine("⚠️  GOAL FAILED: Time > 1000ms");
            }
            else
            {
                Console.WriteLine("✅  GOAL MET: Time < 1000ms");
            }
        }
        
        private static byte[] Compress(DeflateAlgorithm compressor, byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                compressor.CompressAsync(new MemoryStream(data), ms, null, CancellationToken.None).Wait();
                return ms.ToArray();
            }
        }
        
        private static byte[] GenerateTextData(int size)
        {
            var sb = new StringBuilder(size);
            string[] words = { "compression", "algorithm", "performance", "optimization", "fast", "speed", "ratio", "deflate", "lz77", "huffman", "code", "csharp", "dotnet", "framework", "windows", "leads", "canada", "confirm", "email", "address", "phone", "number", "contact", "business", "marketing", "sales", "database", "record", "client", "customer" };
            var rand = new Random(12345);
            
            while (sb.Length < size)
            {
                // Create some repetitive patterns
                if (rand.NextDouble() < 0.3)
                {
                    string phrase = "Common business contact record for customer ";
                    for (int i = 0; i < 5; i++) sb.Append(phrase);
                }
                
                sb.Append(words[rand.Next(words.Length)]);
                sb.Append(" ");
                if (rand.NextDouble() < 0.1) sb.AppendLine();
            }
            
            return Encoding.UTF8.GetBytes(sb.ToString().Substring(0, size));
        }
    }
}
