using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA.Utility;

internal class HashUtils
{
    internal static string ComputeMd5Hex(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);

        return Convert.ToHexStringLower(hash);
    }

    internal static async ValueTask<string> ComputeMd5HexAsync(Stream stream, Action<long>? callback, CancellationToken token = default)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using var md5 = MD5.Create();

        var buffer = new byte[81920];
        long totalBytesRead = 0;

        md5.Initialize();

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
        {
            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            totalBytesRead += bytesRead;
            callback?.Invoke(totalBytesRead);
        }

        md5.TransformFinalBlock(buffer, 0, 0);
        byte[] hash = md5.Hash ?? [];

        return Convert.ToHexStringLower(hash);
    }
}
