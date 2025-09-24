using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Utility;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypos

namespace Hi3Helper.Plugin.DNA.Management;

[GeneratedComClass]
internal partial class DNAGameManager : GameManagerBase
{
    internal DNAGameManager(string gameExecutableNameByPreset,
        string apiResponseBaseUrl)
    {
        CurrentGameExecutableByPreset = gameExecutableNameByPreset;
        ApiResponseBaseUrl = apiResponseBaseUrl;
    }

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

    protected override string ApiResponseBaseUrl { get; }

    private string CurrentGameExecutableByPreset { get; }

    internal string? GameResourceBaseUrl { get; set; }
    internal string? GameResourceBasisPath { get; set; }
    internal bool IsInitialized { get; set; }

    protected override GameVersion CurrentGameVersion => GameVersion.Empty;

    protected override GameVersion ApiGameVersion => GameVersion.Empty;

    protected override bool HasPreload => ApiPreloadGameVersion != GameVersion.Empty && !HasUpdate;
    protected override bool HasUpdate => IsInstalled && ApiGameVersion != CurrentGameVersion;

    protected override bool IsInstalled
    {
        get
        {
            string executablePath1 =
                Path.Combine(CurrentGameInstallPath ?? string.Empty, CurrentGameExecutableByPreset);
            string executablePath2 = Path.Combine(CurrentGameInstallPath ?? string.Empty,
                Path.GetFileNameWithoutExtension(CurrentGameExecutableByPreset), "Client-Win64-ShippingBase.dll");
            string executablePath3 = Path.Combine(CurrentGameInstallPath ?? string.Empty, "EM.exe");
            return File.Exists(executablePath1) && File.Exists(executablePath2) && File.Exists(executablePath3);
        }
    }

    public override void Dispose()
    {
        if (IsDisposed)
            return;

        using (ThisInstanceLock.EnterScope())
        {
            ApiDownloadHttpClient.Dispose();
            ApiDownloadHttpClient = null!;

            base.Dispose();
        }
    }

    protected override void SetCurrentGameVersionInner(in GameVersion gameVersion)
    {
        CurrentGameVersion = gameVersion;
    }

    protected override void SetGamePathInner(string gamePath)
    {
        CurrentGameInstallPath = gamePath;
    }

    protected override Task<int> InitAsync(CancellationToken token)
    {
        return InitAsyncInner(true, token);
    }

    internal async Task<int> InitAsyncInner(bool forceInit = false, CancellationToken token = default)
    {
        if (!forceInit && IsInitialized)
            return 0;

        IsInitialized = true;

        return 0;
    }

    protected override Task DownloadAssetAsyncInner(HttpClient? client, string fileUrl, Stream outputStream,
        PluginDisposableMemory<byte> fileChecksum, PluginFiles.FileReadProgressDelegate? downloadProgress,
        CancellationToken token)
    {
        return base.DownloadAssetAsyncInner(ApiDownloadHttpClient, fileUrl, outputStream, fileChecksum, downloadProgress, token);
    }

    protected override Task<string?> FindExistingInstallPathAsyncInner(CancellationToken token)
        => Task.Factory.StartNew<string?>(() =>
        {
            // NOP
            return null;
        }, token);


    public override void LoadConfig()
    {
        // NOP
    }

    public override void SaveConfig()
    {
        // NOP
    }
}