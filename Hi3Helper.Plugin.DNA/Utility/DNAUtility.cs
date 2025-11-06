using Hi3Helper.Plugin.Core.Utility;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;

#if !USELIGHTWEIGHTJSONPARSER
using System.Threading;
using System.Threading.Tasks;
#endif

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace Hi3Helper.Plugin.DNA.Utility;

internal static class DNAUtility
{
    internal static HttpClient CreateApiHttpClient(bool useCompression = true)
        => CreateApiHttpClientBuilder(useCompression).Create();

    internal static PluginHttpClientBuilder CreateApiHttpClientBuilder(bool useCompression = true)
    {
        PluginHttpClientBuilder builder = new PluginHttpClientBuilder()
            .SetUserAgent($"EMLauncher/++UE4+Release-4.27-CL-0 Windows/10.0.26100.1.768.64bit");

        if (!useCompression)
        {
            builder.SetAllowedDecompression(DecompressionMethods.None);
        }

        builder.AddHeader("Accept", "*/*")
            .AddHeader("Accept-Encoding", "deflate, gzip")
            .AddHeader("Content-Length", "0");

        return builder;
    }

    #region Hash Computation
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
    #endregion
}
