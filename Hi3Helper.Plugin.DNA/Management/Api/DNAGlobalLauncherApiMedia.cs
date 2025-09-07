using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management.Api;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

#if !USELIGHTWEIGHTJSONPARSER
using Microsoft.Extensions.Logging;
using System.Text.Json;
#endif

// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.Api;

[GeneratedComClass]
internal partial class DNAGlobalLauncherApiMedia(string apiResponseBaseUrl) : LauncherApiMediaBase
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
        get => field ??= DNAUtility.CreateApiHttpClient();
        set;
    }

    protected override string ApiResponseBaseUrl { get; } = apiResponseBaseUrl;

    private DNAApiResponseNews? ApiResponse { get; set; }

    public override unsafe void GetBackgroundEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        isDisposable = false;
        handle = nint.Zero;
        count = 0;
        isAllocated = false;
    }

    public override void GetLogoOverlayEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        isDisposable = false;
        handle = nint.Zero;
        count = 0;
        isAllocated = false;
    }

    public override void GetBackgroundFlag(out LauncherBackgroundFlag result)
        => result = LauncherBackgroundFlag.None;

    public override void GetLogoFlag(out LauncherBackgroundFlag result)
        => result = LauncherBackgroundFlag.None;

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        //using HttpResponseMessage message = await ApiResponseHttpClient.GetAsync(ApiResponseBaseUrl + "api/launcher/base/config", HttpCompletionOption.ResponseHeadersRead, token);
        //message.EnsureSuccessStatusCode();

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
            ApiDownloadHttpClient.Dispose();
            ApiDownloadHttpClient = null!;

            ApiResponse = null;
            base.Dispose();
        }
    }
}
