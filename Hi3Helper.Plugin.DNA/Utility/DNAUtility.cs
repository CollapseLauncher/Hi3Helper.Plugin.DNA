using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.Api;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

#if !USELIGHTWEIGHTJSONPARSER
using System.Text.Json;
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
}
