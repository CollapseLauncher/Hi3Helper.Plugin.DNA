using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.Api;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

#if !USELIGHTWEIGHTJSONPARSER
using System.Text.Json;
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

    internal static string ComputeMd5Hex(Stream stream, CancellationToken token = default)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var md5 = MD5.Create();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 << 10); // 64 KiB buffer
        try
        {
            int bytesRead;
            while ((bytesRead = stream.Read(buffer)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            md5.TransformFinalBlock(buffer, 0, 0);

            byte[] hash = md5.Hash!;
            return Convert.ToHexStringLower(hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    internal static async ValueTask<string> ComputeMd5HexAsync(Stream stream, CancellationToken token = default)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var md5 = MD5.Create();

        await md5.ComputeHashAsync(stream, token);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 << 10); // 64 KiB buffer
        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            md5.TransformFinalBlock(buffer, 0, 0);

            byte[] hash = md5.Hash!;
            return Convert.ToHexStringLower(hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
