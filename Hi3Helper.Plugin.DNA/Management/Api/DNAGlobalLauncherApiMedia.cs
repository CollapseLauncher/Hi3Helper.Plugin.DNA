using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management.Api;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.PresetConfig;
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
internal partial class DNAGlobalLauncherApiMedia(DNAPresetConfig presetConfig) : LauncherApiMediaBase
{
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

    private DNAApiDetails ApiDetails { get; } = presetConfig.ApiResponseDetails;

    protected string LogoUrl { get; } = presetConfig.ApiResponseDetails.GetOverlayUrl();
    protected string BackgroundUrl { get; private set; } = presetConfig.DefaultBackgroundUrl;

    public override unsafe void GetBackgroundEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        if (BackgroundUrl == null)
        {
            isDisposable = false;
            handle = nint.Zero;
            count = 0;
            isAllocated = false;
            return;
        }

        SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::GetBackgroundEntries] Background Url is {Url}", BackgroundUrl);

        using (ThisInstanceLock.EnterScope())
        {
            PluginDisposableMemory<LauncherPathEntry> backgroundEntries = PluginDisposableMemory<LauncherPathEntry>.Alloc(1);

            try
            {
                ref LauncherPathEntry entry = ref backgroundEntries[0];
                entry.Write(BackgroundUrl, []);      

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
        return;

        /*if (LogoUrl == null)
        {
            isDisposable = false;
            handle = nint.Zero;
            count = 0;
            isAllocated = false;
            return;
        }

        SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::GetLogoOverlayEntries] Logo Overlay Url is {Url}", LogoUrl);

        using (ThisInstanceLock.EnterScope())
        {
            PluginDisposableMemory<LauncherPathEntry> backgroundEntries = PluginDisposableMemory<LauncherPathEntry>.Alloc(1);

            try
            {
                ref LauncherPathEntry entry = ref backgroundEntries[0];
                entry.Write(LogoUrl, []);

                isAllocated = true;
            }
            finally
            {
                isDisposable = backgroundEntries.IsDisposable == 1;
                handle = backgroundEntries.AsSafePointer();
                count = backgroundEntries.Length;
            }
        }*/
    }

    public override void GetBackgroundFlag(out LauncherBackgroundFlag result)
        => result = LauncherBackgroundFlag.TypeIsImage | LauncherBackgroundFlag.IsSourceFile;

    public override void GetLogoFlag(out LauncherBackgroundFlag result)
        => result = LauncherBackgroundFlag.None /* LauncherBackgroundFlag.TypeIsImage | LauncherBackgroundFlag.IsSourceFile */;

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        try
        {
            var manifestUrl = ApiDetails.GetVersionManifestUrl();
            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::InitAsync] Manifest Url is {Url}", manifestUrl);

            using HttpResponseMessage manifestResponse =
                await ApiResponseHttpClient.GetAsync(manifestUrl, HttpCompletionOption.ResponseHeadersRead, token);
            manifestResponse.EnsureSuccessStatusCode();

            string jsonResponse = await manifestResponse.Content.ReadAsStringAsync(token);

            var apiResponse = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseVersionManifest);

            BackgroundUrl = DNAApiDetails.GetBackgroundUrl(apiResponse!.LatestVersionNumber!);

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::InitAsync] Background Url is {Url}", BackgroundUrl);
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiMedia::InitAsync] Failed to obtain Background Url. Ex: {Message}", ex.Message);
            return -1;
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
