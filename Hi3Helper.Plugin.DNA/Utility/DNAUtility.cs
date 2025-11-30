using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Utility;
using System.Net;
using System.Net.Http;

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
            .AddHeader("Content-Length", "0")
            .AddHeader("Cache-Control", "no-cache, no-store, must-revalidate")
            .AddHeader("Pragma", "no-cache")
            .AddHeader("Expires", "0");

        return builder;
    }

    internal static HttpClient CreateVideoHttpClient(bool useCompression = true)
        => CreateVideoHttpClientBuilder(useCompression).Create();

    internal static PluginHttpClientBuilder CreateVideoHttpClientBuilder(bool useCompression = true)
    {
        PluginHttpClientBuilder builder = new PluginHttpClientBuilder()
            .SetUserAgent($"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0");

        builder.AddHeader("pragma", "no-cache")
            .AddHeader("cache-control", "no-cache")
            .AddHeader("sec-ch-ua-platform", "Windows")
            .AddHeader("accept-encoding", "identity;q=1, *;q=0")
            .AddHeader("sec-ch-ua", "\"Chromium\";v=\"142\", \"Microsoft Edge\";v=\"142\", \"Not_A Brand\";v=\"99\"")
            .AddHeader("dnt", "1")
            .AddHeader("sec-ch-ua-mobile", "?0")
            .AddHeader("accept", "*/*")
            .AddHeader("sec-fetch-site", "cross-site")
            .AddHeader("sec-fetch-mode", "no-cors")
            .AddHeader("sec-fetch-dest", "video")
            .AddHeader("sec-fetch-storage-access", "active")
            .AddHeader("referer", "https://duetnightabyss.dna-panstudio.com/")
            .AddHeader("accept-language", "en,en-GB;q=0.9,en-US;q=0.8,pt-PT;q=0.7,pt;q=0.6,pt-BR;q=0.5,es;q=0.4,fr;q=0.3,zh-CN;q=0.2,zh;q=0.1,it;q=0.1")
            .AddHeader("range", "bytes=0-")
            .AddHeader("priority", "i");

        return builder;
    }

    internal static string GetApiLangFromLauncherLocale() => SharedStatic.PluginLocaleCode switch
    {
        "zh-cn" => "CN",
        "zh-tw" => "TC",
        "ja-jp" => "JP",
        "ko-kr" => "KR",
        _ => "EN",
    };

}
