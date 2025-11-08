using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA.Utility;

internal class HashUtils
{
    internal static string ComputeMd5Hex(Stream stream, CancellationToken token = default)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);

        return Convert.ToHexStringLower(hash);
    }

    internal static async ValueTask<string> ComputeMd5HexAsync(Stream stream, CancellationToken token = default)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using var md5 = MD5.Create();
        byte[] hash = await md5.ComputeHashAsync(stream, token).ConfigureAwait(false);

        return Convert.ToHexStringLower(hash);
    }
}
