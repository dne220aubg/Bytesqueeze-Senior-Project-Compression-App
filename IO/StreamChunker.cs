using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectCompressionApp.IO
{
    // Utility methods for chunked stream copying with cancellation and optional progress callbacks.
    public static class StreamChunker
    {
        private const int DefaultBufferSize = 128 * 1024;

        public static async Task CopyAsync(
            Stream source,
            Stream destination,
            long? maxBytes,
            Action<long>? onBytesCopied,
            CancellationToken cancellationToken,
            int bufferSize = DefaultBufferSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            byte[] buffer = new byte[bufferSize];
            long remaining = maxBytes ?? long.MaxValue;
            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int toRead = remaining > buffer.Length ? buffer.Length : (int)remaining;
                int read = await source.ReadAsync(buffer, 0, toRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                onBytesCopied?.Invoke(read);
                remaining -= read;
            }
        }
    }
}
