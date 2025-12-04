using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SeniorProjectCompressionApp.Compression.Algorithms;

namespace SeniorProjectCompressionApp.Tests
{
    public class StreamedFilePerformanceTest
    {
        public static void Run()
        {
            string filePath = @"C:\Users\Lazaj Store\Desktop\CANADA CONFIRM LEADS (1).txt";
            string outputPath = @"C:\Users\Lazaj Store\Desktop\SeniorProjectCompressionApp\Tests\test_output.deflate";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: File not found: {filePath}");
                return;
            }
            
            FileInfo fileInfo = new FileInfo(filePath);
            Console.WriteLine($"Testing STREAMING compression with: {filePath}");
            Console.WriteLine($"File size: {fileInfo.Length:N0} bytes ({fileInfo.Length / (1024.0 * 1024.0):F2} MB)");
            Console.WriteLine("Using TRUE STREAMING (FileStream -> FileStream, no full file load)\n");
            
            var compressor = new DeflateAlgorithm();
            
            // TRUE STREAMING: FileStream to FileStream (just like your app does)
            var sw = Stopwatch.StartNew();
            
            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920))
            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920))
            {
                compressor.CompressAsync(inputStream, outputStream, null, CancellationToken.None).Wait();
            }
            
            sw.Stop();
            
            FileInfo compressedInfo = new FileInfo(outputPath);
            double ratio = (double)compressedInfo.Length / fileInfo.Length * 100.0;
            
            Console.WriteLine($"=== STREAMING RESULTS ===");
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2}s)");
            Console.WriteLine($"Compressed Size: {compressedInfo.Length:N0} bytes ({compressedInfo.Length / (1024.0 * 1024.0):F2} MB)");
            Console.WriteLine($"Ratio: {ratio:F2}%");
            Console.WriteLine($"Speed: {fileInfo.Length / 1024.0 / sw.Elapsed.TotalSeconds:F0} KB/s");
            Console.WriteLine($"Peak Memory: {GC.GetTotalMemory(false) / (1024.0 * 1024.0):F2} MB (should be low!)");
            
            if (sw.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine($"\n⚠️  TOO SLOW: {sw.ElapsedMilliseconds}ms > 1000ms target");
            }
            else
            {
                Console.WriteLine("\n✅  GOAL MET!");
            }
            
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                Console.WriteLine("(Cleaned up test output file)");
            }
        }
    }
}
