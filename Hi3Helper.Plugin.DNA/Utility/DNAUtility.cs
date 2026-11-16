using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace Hi3Helper.Plugin.DNA.Utility;

internal static class DNAUtility
{
    internal static HttpClient CreateApiHttpClient(bool useCompression = true)
    {
        var client = CreateApiHttpClientBuilder(useCompression).Create();
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.Zero,
            NoStore = true,
            NoCache = true,
            MustRevalidate = true
        };

        return client;
    }

    internal static PluginHttpClientBuilder CreateApiHttpClientBuilder(bool useCompression = true)
    {
        PluginHttpClientBuilder builder = new PluginHttpClientBuilder()
            .SetUserAgent($"EMLauncher/++UE4+Release-4.27-CL-0 Windows/10.0.26200.1.256.64bit");

        if (!useCompression)
        {
            builder.SetAllowedDecompression(DecompressionMethods.None);
        }

        builder.AddHeader("Accept", "*/*")
            .AddHeader("Accept-Encoding", "deflate, gzip")
            .AddHeader("Content-Length", "0")
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
        "fr-fr" => "FR",
        _ => "EN",
    };

    internal static string GetOverlayLangFromLauncherLocale() => SharedStatic.PluginLocaleCode switch
    {
        "zh-cn" => "chs",
        "zh-tw" => "cht",
        "ja-jp" => "jp",
        "ko-kr" => "kr",
        _ => "en",
    };

    #region Plugin Folder
    public static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "CollapseLauncher");
    public static readonly string AppConfigFile = Path.Combine(AppDataFolder, "config.ini");

    internal static string? FindPluginPath()
    {
        if (!Path.Exists(AppConfigFile))
            return null;

        var gameFolder = GetIniValue(AppConfigFile, "GameFolder");
        return Path.Combine(gameFolder!, "_plugins", "Hi3Helper.Plugin.DNAbyss");
    }

    private static string? GetIniValue(string path, string key)
    {
        using var reader = new StreamReader(path);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            ReadOnlySpan<char> s = line.AsSpan().Trim();

            // Skip blanks, comments, and [section] headers
            if (s.IsEmpty || s[0] is ';' or '#' or '[')
                continue;

            int eq = s.IndexOf('=');
            if (eq <= 0) continue;

            if (s[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return s[(eq + 1)..].Trim().ToString();
        }

        return null;
    }
    #endregion

    #region HPatchZ
    public static async Task RunHPatchZAsync(ProcessStartInfo psi, long totalSize, string outputDir,
        Action<long> progressCallback, CancellationToken token)
    {
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        using Process proc = new() { StartInfo = psi };
        proc.Start();

        await using CancellationTokenRegistration reg = token.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
            catch { /* already exited */ }
        });

        Task<string> stdoutTask = proc.StandardOutput.ReadToEndAsync(token);
        Task<string> stderrTask = proc.StandardError.ReadToEndAsync(token);
        Task exitTask = proc.WaitForExitAsync(token);

        long lastBytes = GetDirectorySize(outputDir);

        while (!exitTask.IsCompleted)
        {
            await Task.WhenAny(exitTask, Task.Delay(1000, token));

            long current = GetDirectorySize(outputDir);
            long o = current - lastBytes;
            if (o > 0)
            {
                lastBytes = current;
                progressCallback(o);
            }
        }

        await exitTask;

        if (proc.ExitCode != 0)
            throw new Exception(await stderrTask);

        long end = GetDirectorySize(outputDir);
        if (end - lastBytes > 0)
        {
            progressCallback(end - lastBytes);
        }
    }

    private static long GetDirectorySize(string path)
    {
        long total = 0;
        try
        {
            DirectoryInfo di = new(path);
            if (!di.Exists) return 0;

            foreach (FileInfo fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try { total += fi.Length; }
                catch (IOException) { /* file vanished or locked, skip */ }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return total;
    }
    #endregion
}
