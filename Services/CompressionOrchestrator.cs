using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;

using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Models;
using SeniorProjectCompressionApp.Security;

namespace SeniorProjectCompressionApp.Services
{
    public sealed class CompressionOrchestrator : ICompressionOrchestrator
    {
        public const string DefaultArchiveExtension = ".spca";

        private readonly ICompressionAlgorithmRegistry _registry;
        private readonly IFileSystemService _fileSystem;
        private readonly IArchiveSerializer _serializer;
        private readonly IEncryptionService _encryptionService;

        private static readonly byte[] RawContainerHeader = Encoding.ASCII.GetBytes("SPCR1");
        private static readonly byte[] EncryptedContainerHeader = Encoding.ASCII.GetBytes("SPCE"); // Encrypted Magic Header
        private const int StreamingFormatVersion = 2;

        public CompressionOrchestrator(
            ICompressionAlgorithmRegistry registry,
            IFileSystemService fileSystem,
            IArchiveSerializer serializer,
            IEncryptionService encryptionService)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        public async Task<CompressionSummary> CompressAsync(
            string inputPath,
            string algorithmName,
            string? password,
            string? outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(inputPath)) throw new ArgumentException("Input path must be provided.", nameof(inputPath));
            if (string.IsNullOrWhiteSpace(algorithmName)) throw new ArgumentException("Algorithm name must be provided.", nameof(algorithmName));

            ICompressionAlgorithm? algorithm = _registry.GetAlgorithm(algorithmName);
            if (algorithm == null) throw new InvalidOperationException($"The algorithm '{algorithmName}' is not registered.");

            bool isDirectory = Directory.Exists(inputPath);
            bool isFile = File.Exists(inputPath);

            if (!isFile && !isDirectory) throw new FileNotFoundException("The specified input path could not be found.", inputPath);

            string normalizedPath = isDirectory ? new DirectoryInfo(inputPath).FullName : new FileInfo(inputPath).FullName;
            string rootName = isDirectory ? new DirectoryInfo(normalizedPath).Name : Path.GetFileName(normalizedPath);

            // Check for streaming support
            if (algorithm is IStreamingCompressionAlgorithm streamingAlgorithm)
            {
                return await CompressStreamAsync(
                    normalizedPath,
                    rootName,
                    isDirectory,
                    streamingAlgorithm,
                    password,
                    outputPath,
                    progress,
                    cancellationToken).ConfigureAwait(false);
            }

            // Legacy payload-based compression
            PayloadBuildResult payloadResult = await BuildPayloadAsync(normalizedPath, isDirectory, progress, cancellationToken).ConfigureAwait(false);
            ReportProgress(progress, 0.55);
            cancellationToken.ThrowIfCancellationRequested();

            Stopwatch compressionStopwatch = Stopwatch.StartNew();
            CompressionResult initialResult = algorithm.Compress(payloadResult.Payload, cancellationToken);
            byte[] compressedPayload = initialResult.CompressedData;
            compressionStopwatch.Stop();
            ReportProgress(progress, 0.8);

            byte[] finalData = compressedPayload;
            bool isEncrypted = !string.IsNullOrEmpty(password);

            if (isEncrypted)
            {
                finalData = _encryptionService.Encrypt(finalData, password!, cancellationToken);
            }

            CompressionMetadata metadata = initialResult.Metadata;

            ArchiveManifest manifest = new ArchiveManifest(
                rootName,
                payloadResult.Entries,
                isDirectory);

            CompressionResult result = new CompressionResult(metadata, finalData);
            ArchivePackage package = new ArchivePackage(manifest, result, isEncrypted);

            string destinationPath = string.IsNullOrWhiteSpace(outputPath) 
                ? _fileSystem.GetSafeOutputPath(inputPath, DefaultArchiveExtension) 
                : outputPath!;

            byte[] serialized = _serializer.Serialize(package);
            ReportProgress(progress, 0.9);

            await _fileSystem.WriteFileAsync(destinationPath, serialized, cancellationToken).ConfigureAwait(false);
            ReportProgress(progress, 1.0);

            long originalBytes = payloadResult.Entries.Where(entry => !entry.IsDirectory).Sum(entry => entry.OriginalLength);
            int fileCount = payloadResult.Entries.Count(entry => !entry.IsDirectory);
            long archiveBytes = serialized.LongLength;

            return new CompressionSummary(destinationPath, algorithm.Name, originalBytes, archiveBytes, fileCount, isEncrypted, compressionStopwatch.ElapsedMilliseconds);
        }

        private async Task<CompressionSummary> CompressStreamAsync(
            string normalizedPath,
            string rootName,
            bool isDirectory,
            IStreamingCompressionAlgorithm algorithm,
            string? password,
            string? outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            string destinationPath = string.IsNullOrWhiteSpace(outputPath) 
                ? _fileSystem.GetSafeOutputPath(normalizedPath, DefaultArchiveExtension) 
                : outputPath!;

            Stopwatch sw = Stopwatch.StartNew();
            long originalBytes = 0;
            long archiveBytes = 0;
            int fileCount = 0;
            bool isEncrypted = !string.IsNullOrEmpty(password);

            using (FileStream outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                // Write Header
                await outputStream.WriteAsync(RawContainerHeader, 0, RawContainerHeader.Length, cancellationToken);
                
                // Write Metadata (Version, Encrypted, Algorithm)
                using (BinaryWriter writer = new BinaryWriter(outputStream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(StreamingFormatVersion); // Version
                    writer.Write(isEncrypted);
                    writer.Write(algorithm.Name);
                    writer.Write(rootName);
                    writer.Write(isDirectory);
                }

                Stream dataStream = outputStream;
                CryptoStream? cryptoStream = null;

                if (isEncrypted)
                {
                    // Setup encryption
                    cryptoStream = CreateEncryptionStream(outputStream, password!, cancellationToken);
                    dataStream = cryptoStream;
                    
                    // Write Encrypted Header for password verification
                    await dataStream.WriteAsync(EncryptedContainerHeader, 0, EncryptedContainerHeader.Length, cancellationToken);
                }

                // Discover files
                var entries = new List<ArchiveEntry>();
                var filePaths = new List<string>();

                if (isDirectory)
                {
                    string[] files = Directory.GetFiles(normalizedPath, "*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        string relPath = PathUtilities.GetRelativePath(normalizedPath, file);
                        long len = new FileInfo(file).Length;
                        entries.Add(new ArchiveEntry(relPath, false, len));
                        filePaths.Add(file);
                        originalBytes += len;
                    }
                    // Add directories
                    string[] dirs = Directory.GetDirectories(normalizedPath, "*", SearchOption.AllDirectories);
                    foreach (string dir in dirs)
                    {
                        string relPath = PathUtilities.GetRelativePath(normalizedPath, dir);
                        entries.Add(new ArchiveEntry(relPath, true, 0));
                    }
                }
                else
                {
                    long len = new FileInfo(normalizedPath).Length;
                    entries.Add(new ArchiveEntry(Path.GetFileName(normalizedPath), false, len));
                    filePaths.Add(normalizedPath);
                    originalBytes += len;
                }

                fileCount = entries.Count(e => !e.IsDirectory);

                // Write Archive Content (Hybrid Parallel)
                await WriteStreamingArchiveAsync(
                    entries, 
                    filePaths, 
                    dataStream, 
                    algorithm, 
                    isEncrypted, 
                    progress, 
                    originalBytes, 
                    cancellationToken);

                if (cryptoStream != null)
                {
                    if (!cryptoStream.HasFlushedFinalBlock) cryptoStream.FlushFinalBlock();
                    cryptoStream.Dispose();
                }

                archiveBytes = outputStream.Length;
            }

            sw.Stop();
            return new CompressionSummary(destinationPath, algorithm.Name, originalBytes, archiveBytes, fileCount, isEncrypted, sw.ElapsedMilliseconds);
        }

        private CryptoStream CreateEncryptionStream(Stream outputStream, string password, CancellationToken cancellationToken)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var derive = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA256))
            {
                byte[] key = derive.GetBytes(32);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    
                    // Write Salt and IV
                    outputStream.Write(salt, 0, salt.Length);
                    outputStream.Write(aes.IV, 0, aes.IV.Length);

                    return new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
                }
            }
        }

        private async Task WriteStreamingArchiveAsync(
            List<ArchiveEntry> entries,
            List<string> filePaths,
            Stream dataStream,
            IStreamingCompressionAlgorithm algorithm,
            bool isEncrypted,
            IProgress<double>? progress,
            long totalBytes,
            CancellationToken cancellationToken)
        {
            long processedBytes = 0;
            
            using (BinaryWriter writer = new BinaryWriter(dataStream, Encoding.UTF8, leaveOpen: true))
            {
                // Write entry count
                writer.Write(entries.Count);
                writer.Flush();

                // Hybrid Parallel/Sequential Implementation
                var fileEntries = entries.Where(e => !e.IsDirectory).ToList();
                var dirEntries = entries.Where(e => e.IsDirectory).ToList();
                
                // Write directories first (fast)
                foreach (var dir in dirEntries)
                {
                    writer.Write(dir.RelativePath);
                    writer.Write(true); // IsDirectory
                    writer.Write(0L); // OriginalLength
                    writer.Write(0L); // CompressedLength
                }
                writer.Flush();

                int totalFiles = fileEntries.Count;
                int currentIndex = 0;
                int maxParallelism = Environment.ProcessorCount;
                long maxSafeMemoryFileSize = 10 * 1024 * 1024; // 10 MB limit for RAM buffering

                while (currentIndex < totalFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ArchiveEntry currentEntry = fileEntries[currentIndex];
                    string currentPath = filePaths[entries.IndexOf(currentEntry)]; // Find correct path

                    // DECISION: Sequential or Parallel?
                    bool isLargeFile = currentEntry.OriginalLength > maxSafeMemoryFileSize;

                    if (isLargeFile)
                    {
                        // --- SEQUENTIAL PATH (Safe for large files) ---
                        long bytesProcessed = await ProcessSingleFileSequentialAsync(
                            currentEntry, 
                            currentPath, 
                            writer, 
                            dataStream, 
                            algorithm, 
                            isEncrypted, 
                            progress, 
                            totalBytes, 
                            processedBytes, 
                            cancellationToken);
                        
                        processedBytes += bytesProcessed;
                        currentIndex++;
                    }
                    else
                    {
                        // --- PARALLEL PATH (Fast for small files) ---
                        var batchEntries = new List<ArchiveEntry>();
                        var batchPaths = new List<string>();
                        long currentBatchSize = 0;

                        while (currentIndex < totalFiles)
                        {
                            var e = fileEntries[currentIndex];
                            if (e.OriginalLength > maxSafeMemoryFileSize) break;

                            batchEntries.Add(e);
                            batchPaths.Add(filePaths[entries.IndexOf(e)]);
                            currentBatchSize += e.OriginalLength;
                            currentIndex++;

                            if (batchEntries.Count >= maxParallelism * 2) break;
                            if (currentBatchSize >= 100 * 1024 * 1024) break; // 100MB batch limit
                        }

                        if (batchEntries.Count == 0) continue;

                        // Run Parallel Compression
                        var tasks = batchEntries.Select((entry, index) => 
                        {
                            string p = batchPaths[index];
                            return Task.Run(async () => 
                            {
                                using (var ms = new MemoryStream())
                                {
                                    using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                                    {
                                        await algorithm.CompressAsync(fs, ms, cancellationToken);
                                    }
                                    return (ms.ToArray(), ms.Length);
                                }
                            });
                        }).ToList();

                        var results = await Task.WhenAll(tasks);

                        // Write results sequentially
                        for (int i = 0; i < batchEntries.Count; i++)
                        {
                            var entry = batchEntries[i];
                            var result = results[i];

                            writer.Write(entry.RelativePath);
                            writer.Write(false); // IsDirectory
                            writer.Write(entry.OriginalLength);
                            writer.Write(result.Item2); // Length
                            writer.Flush();

                            await dataStream.WriteAsync(result.Item1, 0, result.Item1.Length, cancellationToken); // Data
                            // await dataStream.FlushAsync(cancellationToken); // Avoid flushing CryptoStream mid-stream

                            processedBytes += entry.OriginalLength;
                            double fraction = 0.1 + (0.85 * processedBytes / (double)totalBytes);
                            ReportProgress(progress, Math.Min(0.99, fraction));
                        }
                    }
                }
            }
        }

        private async Task<long> ProcessSingleFileSequentialAsync(
            ArchiveEntry entry,
            string fullPath,
            BinaryWriter writer,
            Stream dataStream,
            IStreamingCompressionAlgorithm algorithm,
            bool isEncrypted,
            IProgress<double>? progress,
            long totalBytes,
            long currentProcessedBytes,
            CancellationToken cancellationToken)
        {
            writer.Write(entry.RelativePath);
            writer.Write(false);
            writer.Write(entry.OriginalLength);

            long compressedLength;

            long localProcessed = currentProcessedBytes;
            Action<long> updateProgress = (bytesRead) => 
            {
                long total = Interlocked.Add(ref localProcessed, bytesRead);
                double fraction = 0.1 + (0.85 * total / (double)totalBytes);
                ReportProgress(progress, Math.Min(0.99, fraction));
            };

            if (!isEncrypted && dataStream.CanSeek)
            {
                writer.Flush();
                long lengthPosition = dataStream.Position;
                writer.Write(0L); // Placeholder
                writer.Flush();
                long startPosition = dataStream.Position;

                using (FileStream input = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                {
                    var progressStream = new ProgressReadStream(input, bytesRead => updateProgress(bytesRead));
                    await algorithm.CompressAsync(progressStream, dataStream, cancellationToken).ConfigureAwait(false);
                    await dataStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                long endPosition = dataStream.Position;
                compressedLength = endPosition - startPosition;

                long returnPosition = dataStream.Position;
                dataStream.Position = lengthPosition;
                writer.Write(compressedLength);
                writer.Flush();
                dataStream.Position = returnPosition;
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    using (FileStream input = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                    {
                        var progressStream = new ProgressReadStream(input, bytesRead => updateProgress(bytesRead));
                        await algorithm.CompressAsync(progressStream, ms, cancellationToken).ConfigureAwait(false);
                    }
                    compressedLength = ms.Length;
                    writer.Write(compressedLength);
                    writer.Flush();
                    ms.Position = 0;
                    await ms.CopyToAsync(dataStream, 81920, cancellationToken);
                }
            }

            double fraction = 0.1 + (0.85 * (currentProcessedBytes + entry.OriginalLength) / (double)totalBytes);
            ReportProgress(progress, Math.Min(0.99, fraction));
            
            return entry.OriginalLength;
        }

        public async Task<DecompressionSummary> DecompressAsync(
            string archivePath,
            string destinationDirectory,
            string? password,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(archivePath)) throw new ArgumentException("Archive path must be provided.", nameof(archivePath));
            if (string.IsNullOrWhiteSpace(destinationDirectory)) throw new ArgumentException("Destination directory must be provided.", nameof(destinationDirectory));
            if (!File.Exists(archivePath)) throw new FileNotFoundException("The archive file could not be found.", archivePath);

            Directory.CreateDirectory(destinationDirectory);
            Directory.CreateDirectory(destinationDirectory);

            // Check for Streaming Format
            using (FileStream fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] header = new byte[RawContainerHeader.Length];
                int read = fs.Read(header, 0, header.Length);
                
                if (read == header.Length && header.SequenceEqual(RawContainerHeader))
                {
                    // Streaming Archive
                    fs.Position = 0; // Rewind to start for the method
                    return await DecompressStreamingArchiveAsync(fs, destinationDirectory, password, progress, cancellationToken).ConfigureAwait(false);
                }
            }

            // Legacy Decompression
            byte[] archiveBytes = await _fileSystem.ReadFileAsync(archivePath, cancellationToken).ConfigureAwait(false);
            ReportProgress(progress, 0.2);

            ArchivePackage package = _serializer.Deserialize(archiveBytes);
            ReportProgress(progress, 0.3);

            CompressionMetadata metadata = package.CompressionResult.Metadata;
            ICompressionAlgorithm? algorithm = _registry.GetAlgorithm(metadata.AlgorithmName);
            if (algorithm == null) throw new InvalidOperationException($"The algorithm '{metadata.AlgorithmName}' is not registered.");

            byte[] storedData = package.CompressionResult.CompressedData;
            if (package.IsEncrypted)
            {
                if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("A password is required to decrypt this archive.");
                storedData = _encryptionService.Decrypt(storedData, password, cancellationToken);
            }

            ReportProgress(progress, 0.45);
            Stopwatch decompressionStopwatch = Stopwatch.StartNew();
            byte[] payload = algorithm.Decompress(metadata, storedData, cancellationToken);
            decompressionStopwatch.Stop();
            ReportProgress(progress, 0.6);

            string targetRoot = package.Manifest.IsDirectory
                ? Path.Combine(destinationDirectory, package.Manifest.RootName)
                : Path.Combine(destinationDirectory, package.Manifest.RootName);

            if (package.Manifest.IsDirectory) Directory.CreateDirectory(targetRoot);
            else { string? parent = Path.GetDirectoryName(targetRoot); if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent); }

            await RestoreEntriesAsync(package.Manifest, payload, destinationDirectory, targetRoot, progress, cancellationToken).ConfigureAwait(false);
            ReportProgress(progress, 1.0);

            long restoredBytes = package.Manifest.Entries.Where(entry => !entry.IsDirectory).Sum(entry => entry.OriginalLength);
            int fileCount = package.Manifest.Entries.Count(entry => !entry.IsDirectory);
            long archiveSize = archiveBytes.LongLength;

            return new DecompressionSummary(targetRoot, metadata.AlgorithmName ?? algorithm.Name, archiveSize, restoredBytes, fileCount, package.IsEncrypted, decompressionStopwatch.ElapsedMilliseconds);
        }

        private async Task<DecompressionSummary> DecompressStreamingArchiveAsync(
            Stream stream,
            string destinationDirectory,
            string? password,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            Stopwatch sw = Stopwatch.StartNew();
            long archiveSize = stream.Length;
            long restoredBytes = 0;
            int fileCount = 0;
            string algorithmName = "Unknown";
            bool wasEncrypted = false;
            string targetRoot = destinationDirectory;

            // Use BinaryReader but be careful with stream ownership
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Verify Header (again)
                byte[] header = reader.ReadBytes(RawContainerHeader.Length);
                if (!header.SequenceEqual(RawContainerHeader)) throw new InvalidDataException("Invalid header.");

                int version = reader.ReadInt32();
                if (version < 1 || version > StreamingFormatVersion)
                {
                    throw new InvalidDataException($"Unsupported streaming archive version: {version}.");
                }
                bool isEncrypted = reader.ReadBoolean();
                wasEncrypted = isEncrypted;
                algorithmName = reader.ReadString();
                string rootName = reader.ReadString();
                bool isDirectory = reader.ReadBoolean();

                IStreamingCompressionAlgorithm? algorithm = _registry.GetAlgorithm(algorithmName) as IStreamingCompressionAlgorithm;
                if (algorithm == null) throw new InvalidOperationException($"Algorithm '{algorithmName}' not found or does not support streaming.");

                Stream dataStream = stream;
                CryptoStream? cryptoStream = null;

                if (isEncrypted)
                {
                    if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Password required.");
                    
                    byte[] salt = new byte[16];
                    if (stream.Read(salt, 0, 16) != 16) throw new EndOfStreamException("Invalid Salt");
                    
                    byte[] iv = new byte[16];
                    if (stream.Read(iv, 0, 16) != 16) throw new EndOfStreamException("Invalid IV");

                    using (var derive = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA256))
                    {
                        byte[] key = derive.GetBytes(32);
                        using (Aes aes = Aes.Create())
                        {
                            aes.Key = key;
                            aes.IV = iv;
                            cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true);
                            dataStream = cryptoStream;
                        }
                    }
                }

                if (isEncrypted)
                {
                    try
                    {
                        // Verify Encrypted Header
                        byte[] encHeader = new byte[EncryptedContainerHeader.Length];
                        int read = 0;
                        while (read < encHeader.Length)
                        {
                            int r = await dataStream.ReadAsync(encHeader, read, encHeader.Length - read, cancellationToken);
                            if (r == 0) break;
                            read += r;
                        }

                        if (read != encHeader.Length || !encHeader.SequenceEqual(EncryptedContainerHeader))
                        {
                            throw new InvalidOperationException("Invalid password provided.");
                        }
                    }
                    catch (Exception ex) when (ex is not InvalidOperationException && ex is not OperationCanceledException)
                    {
                        // Catch any crypto/padding errors and wrap them
                        throw new InvalidOperationException("Invalid password provided.", ex);
                    }
                }

                // Read Entries
                // Use helpers directly on dataStream to avoid buffering issues
                int entryCount = await ReadInt32Async(dataStream);
                bool hasLegacyRawFlag = version == 1;
                
                for (int i = 0; i < entryCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    string relativePath = await ReadStringAsync(dataStream);
                    bool isDir = await ReadBooleanAsync(dataStream);
                    if (hasLegacyRawFlag)
                    {
                        await ReadBooleanAsync(dataStream); // Discard legacy raw flag.
                    }
                    long originalLength = await ReadInt64Async(dataStream);
                    _ = await ReadInt64Async(dataStream); // Compressed length (not needed for decompression boundary).
                    
                    string fullPath = Path.Combine(destinationDirectory, rootName, relativePath);
                    if (!isDirectory && i == 0) fullPath = Path.Combine(destinationDirectory, rootName); // Single file case

                    if (isDir)
                    {
                        Directory.CreateDirectory(fullPath);
                        continue;
                    }
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    
                    using (FileStream output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        long fileTotalBytes = originalLength;
                        long fileProcessedBytes = 0;

                        using (var progressOutput = new ProgressWriteStream(output, bytes =>
                        {
                            fileProcessedBytes += bytes;
                            restoredBytes += bytes;

                            double fileFraction = fileTotalBytes > 0 ? (double)fileProcessedBytes / fileTotalBytes : 1.0;
                            double globalFraction = (i + fileFraction) / entryCount;
                            ReportProgress(progress, globalFraction);
                        }))
                        {
                            await algorithm.DecompressAsync(dataStream, progressOutput, cancellationToken);
                        }
                    }
                    
                    fileCount++;
                    
                    ReportProgress(progress, (double)(i + 1) / entryCount);
                }
                
                if (cryptoStream != null) cryptoStream.Dispose();
            }
            
            sw.Stop();
            return new DecompressionSummary(targetRoot, algorithmName, archiveSize, restoredBytes, fileCount, wasEncrypted, sw.ElapsedMilliseconds);
        }

        private async Task<PayloadBuildResult> BuildPayloadAsync(string normalizedPath, bool isDirectory, IProgress<double>? progress, CancellationToken cancellationToken)
        {
            List<ArchiveEntry> entries = new List<ArchiveEntry>();

            if (!isDirectory)
            {
                FileInfo fileInfo = new FileInfo(normalizedPath);
                byte[] fileData = await _fileSystem.ReadFileAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
                entries.Add(new ArchiveEntry(fileInfo.Name, false, fileInfo.Length));
                return new PayloadBuildResult(fileData, entries);
            }

            string rootPath = normalizedPath;
            string[] files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);

            using (MemoryStream payload = new MemoryStream())
            {
                foreach (string file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    FileInfo info = new FileInfo(file);
                    string relativePath = PathUtilities.GetRelativePath(rootPath, file);
                    byte[] data = await _fileSystem.ReadFileAsync(file, cancellationToken).ConfigureAwait(false);
                    entries.Add(new ArchiveEntry(relativePath, false, info.Length));
                    payload.Write(data, 0, data.Length);
                }
                return new PayloadBuildResult(payload.ToArray(), entries);
            }
        }

        private async Task RestoreEntriesAsync(ArchiveManifest manifest, byte[] payload, string destinationDirectory, string targetRoot, IProgress<double>? progress, CancellationToken cancellationToken)
        {
            long position = 0;

            foreach (ArchiveEntry entry in manifest.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = entry.RelativePath.Replace('/', Path.DirectorySeparatorChar);
                string targetPath = manifest.IsDirectory ? Path.Combine(targetRoot, relativePath) : Path.Combine(destinationDirectory, relativePath);

                if (entry.IsDirectory) { Directory.CreateDirectory(targetPath); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? targetRoot);

                int length = (int)entry.OriginalLength;
                byte[] fileData = new byte[length];
                Buffer.BlockCopy(payload, (int)position, fileData, 0, length);
                position += length;
                await _fileSystem.WriteFileAsync(targetPath, fileData, cancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class PayloadBuildResult
        {
            public PayloadBuildResult(byte[] payload, List<ArchiveEntry> entries)
            {
                Payload = payload;
                Entries = entries;
            }
            public byte[] Payload { get; }
            public List<ArchiveEntry> Entries { get; }
        }

        private static void ReportProgress(IProgress<double>? progress, double value)
        {
            progress?.Report(Math.Max(0, Math.Min(1, value)));
        }

        private sealed class ProgressReadStream : Stream
        {
            private readonly Stream _inner;
            private readonly Action<int> _onRead;
            public ProgressReadStream(Stream inner, Action<int> onRead) { _inner = inner; _onRead = onRead; }
            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = _inner.Read(buffer, offset, count);
                _onRead(read);
                return read;
            }
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private sealed class ProgressWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly Action<int> _onWrite;
            public ProgressWriteStream(Stream inner, Action<int> onWrite) { _inner = inner; _onWrite = onWrite; }
            public override bool CanRead => false;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
                _onWrite(count);
            }
            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                _onWrite(count);
            }
        }
        private static async Task<int> ReadInt32Async(Stream stream)
        {
            byte[] buffer = new byte[4];
            int read = 0;
            while (read < 4)
            {
                int r = await stream.ReadAsync(buffer, read, 4 - read);
                if (r == 0) throw new EndOfStreamException();
                read += r;
            }
            return BitConverter.ToInt32(buffer, 0);
        }

        private static async Task<long> ReadInt64Async(Stream stream)
        {
            byte[] buffer = new byte[8];
            int read = 0;
            while (read < 8)
            {
                int r = await stream.ReadAsync(buffer, read, 8 - read);
                if (r == 0) throw new EndOfStreamException();
                read += r;
            }
            return BitConverter.ToInt64(buffer, 0);
        }

        private static async Task<bool> ReadBooleanAsync(Stream stream)
        {
            byte[] buffer = new byte[1];
            if (await stream.ReadAsync(buffer, 0, 1) == 0) throw new EndOfStreamException();
            return buffer[0] != 0;
        }

        private static async Task<string> ReadStringAsync(Stream stream)
        {
            int length = await Read7BitEncodedIntAsync(stream);
            byte[] buffer = new byte[length];
            int read = 0;
            while (read < length)
            {
                int r = await stream.ReadAsync(buffer, read, length - read);
                if (r == 0) throw new EndOfStreamException();
                read += r;
            }
            return Encoding.UTF8.GetString(buffer);
        }

        private static async Task<int> Read7BitEncodedIntAsync(Stream stream)
        {
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                if (shift == 35) throw new FormatException("Bad 7-bit int");
                byte[] buf = new byte[1];
                if (await stream.ReadAsync(buf, 0, 1) == 0) throw new EndOfStreamException();
                b = buf[0];
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }
}
