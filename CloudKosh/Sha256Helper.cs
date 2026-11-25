
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudKosh;


public static class Sha256Helper
{
    public static async Task<byte[]> ComputeSha256Async(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        // Rent a 8KB buffer from the shared pool to avoid LOH/heap allocations
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using (var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    .ConfigureAwait(false)) > 0)
                {
                    hasher.AppendData(buffer, 0, bytesRead);
                }

                return hasher.GetHashAndReset();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}