using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management.Api;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.Api;

[GeneratedComClass]
internal partial class DNAGlobalLauncherApiMedia(DNAApiResponseDetails apiResponseDetails, string logoUrl, string backgroundUrl, string steamUrl) : LauncherApiMediaBase
{
    private DNAApiResponseSteamData? steamData = null;

    [field: AllowNull, MaybeNull]
    protected override HttpClient ApiResponseHttpClient
    {
        get => field ??= DNAUtility.CreateApiHttpClient();
        set;
    }

    [field: AllowNull, MaybeNull]
    protected HttpClient ApiDownloadHttpClient
    {
        get => field ??= DNAUtility.CreateVideoHttpClient();
        set;
    }

    protected override string ApiResponseBaseUrl { get; } = apiResponseDetails.BaseUrl;

    protected string LogoUrl { get; } = logoUrl;
    protected string BackgroundUrl { get; } = backgroundUrl;

    public override unsafe void GetBackgroundEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        if (BackgroundUrl == null && (steamData?.Screenshots == null || steamData?.Screenshots?.Count == 0))
        {
            isDisposable = false;
            handle = nint.Zero;
            count = 0;
            isAllocated = false;
            return;
        }

        var totalCount = steamData?.Screenshots?.Count ?? 1;

        using (ThisInstanceLock.EnterScope())
        {
            PluginDisposableMemory<LauncherPathEntry> backgroundEntries = PluginDisposableMemory<LauncherPathEntry>.Alloc(totalCount);

            try
            {
                if (steamData?.Screenshots != null && steamData?.Screenshots?.Count > 0)
                {
                    for (int i = 0; i < steamData?.Screenshots.Count; i++)
                    {
                        ref LauncherPathEntry entry = ref backgroundEntries[i];
                        entry.Write(steamData.Screenshots[i].PathFull, []);
                    }
                }
                else
                {
                    ref LauncherPathEntry entry = ref backgroundEntries[0];
                    entry.Write(BackgroundUrl, []);      
                }

                isAllocated = true;
            }
            finally
            {
                isDisposable = backgroundEntries.IsDisposable == 1;
                handle = backgroundEntries.AsSafePointer();
                count = backgroundEntries.Length;
            }
        }
    }

    public override void GetLogoOverlayEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        isDisposable = false;
        handle = nint.Zero;
        count = 0;
        isAllocated = false;
    }

    public override void GetBackgroundFlag(out LauncherBackgroundFlag result)
        => result = LauncherBackgroundFlag.TypeIsImage | LauncherBackgroundFlag.IsSourceFile;

    public override void GetLogoFlag(out LauncherBackgroundFlag result)
        => result = LauncherBackgroundFlag.None;

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        try
        {
            var apiSteamUrl = $"{steamUrl}{DNAUtility.GetSteamLangFromLauncherLocale()}";
            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::InitAsync] Steam Url is {Url}", apiSteamUrl);

            using HttpResponseMessage steamResponse =
                await ApiResponseHttpClient.GetAsync(apiSteamUrl, HttpCompletionOption.ResponseHeadersRead, token);
            steamResponse.EnsureSuccessStatusCode();

            string jsonSteamResponse = await steamResponse.Content.ReadAsStringAsync(token);

            var apiResponseSteam = JsonSerializer.Deserialize(jsonSteamResponse, DNAApiResponseContext.Default.DNAApiResponseSteam);

            steamData = apiResponseSteam?["3950020"].Data;

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::InitAsync] Was it able to read Steam data? Header: {Header}", steamData?.HeaderImage);
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::InitAsync] Failed to read Steam data. Ex: {Message}", ex.Message);
        }

        return 0;
    }

    protected override async Task DownloadAssetAsyncInner(HttpClient? client, string fileUrl, Stream outputStream, PluginDisposableMemory<byte> fileChecksum, PluginFiles.FileReadProgressDelegate? downloadProgress, CancellationToken token)
    {
        await base.DownloadAssetAsyncInner(ApiDownloadHttpClient, fileUrl, outputStream, fileChecksum, downloadProgress, token);
    }

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        using (ThisInstanceLock.EnterScope())
        {
            base.Dispose();
        }
    }
}
