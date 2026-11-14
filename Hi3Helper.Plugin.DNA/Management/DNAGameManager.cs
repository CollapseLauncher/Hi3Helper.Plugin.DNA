using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.Api;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using Hi3Helper.Plugin.DNA.Management.PresetConfig;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypos
#pragma warning disable CA1416 // Incompatible Platform

namespace Hi3Helper.Plugin.DNA.Management;

[GeneratedComClass]
internal partial class DNAGameManager : GameManagerBase
{
    internal DNAGameManager(
        string gameExecutableNameByPreset,
        DNAPresetConfig preset)
    {
        CurrentGameExecutableByPreset = gameExecutableNameByPreset;
        ApiDetails = preset.ApiResponseDetails;
        Preset = preset;
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

    private DNAPresetConfig Preset { get; }

    public DNAApiDetails ApiDetails { get; }

    private string CurrentGameExecutableByPreset { get; }
    
    internal bool IsInitialized { get; set; }

    protected override GameVersion ApiGameVersion { get; set; }
    protected override GameVersion CurrentGameVersion { get; set; }

    protected override bool HasPreload => false;
    protected override bool HasUpdate => IsInstalled && ApiGameVersion != CurrentGameVersion;

    public DNAApiResponseVersionManifest? VersionManifest { get; private set; } = null;
    public DNAFilesGameVersion? GameVersion { get; private set; } = null;
    public DNAFilesDownloadVersion? DownloadVersion { get; private set; } = null;

    protected override bool IsInstalled
    {
        get
        {
            string executablePath1 =
                Path.Combine(CurrentGameInstallPath ?? string.Empty, CurrentGameExecutableByPreset);
            string executablePath2 = Path.Combine(CurrentGameInstallPath ?? string.Empty, "EM/Binaries/Win64/EM-Win64-Shipping.exe");
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

        using HttpResponseMessage manifestResponse =
            await ApiResponseHttpClient.GetAsync(ApiDetails.GetVersionManifestUrl(), HttpCompletionOption.ResponseHeadersRead, token);
        manifestResponse.EnsureSuccessStatusCode();

        string jsonResponse = await manifestResponse.Content.ReadAsStringAsync(token);
        VersionManifest = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseVersionManifest);

        ApiGameVersion = new GameVersion($"{VersionManifest?.LatestVersion}.0.0");
#if DEBUG
        SharedStatic.InstanceLogger.LogTrace("[DNAGameManager::InitAsyncInner] Current Game Version is {Version} ({FormattedVersion})",
            VersionManifest?.LatestVersion, VersionManifest?.LatestVersionNumber);
        SharedStatic.InstanceLogger.LogTrace("[DNAGameManager::InitAsyncInner] Game can be updated from any version over {MinVersion}",
            VersionManifest?.MinSupportedVersion);
#endif

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
            var keyName = $"Software\\{Preset.GameVendorName}\\{Preset.GameRegistryKeyName}";
            var key = Registry.CurrentUser.OpenSubKey(keyName);
            if (key == null)
                return null;

            foreach (var value in key.GetValueNames())
            {
                if (value.StartsWith("CollapseLauncher"))
                    continue;

                if (!Path.Exists(value))
                    continue;

                var launcherDir = Path.GetDirectoryName(value);
                if (launcherDir == null)
                    continue;

                var exePath = $"{Preset.LauncherGameDirectoryName}\\{Preset.StartExecutableName}";
                var expectedExe = Path.Combine(launcherDir, exePath);

                if (!File.Exists(expectedExe))
                    continue;

#if DEBUG
                SharedStatic.InstanceLogger.LogTrace("[DNAGameManager::FindExistingInstallPathAsyncInner] Found game executable at {Path}", expectedExe);
#endif
                return Path.GetDirectoryName(expectedExe);
            }

            return null;
        }, token);


    public override void LoadConfig()
    {
        if (string.IsNullOrEmpty(CurrentGameInstallPath))
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameManager::LoadConfig] Game directory isn't set! Game config won't be loaded.");
            return;
        }

        string filePath = Path.Combine(CurrentGameInstallPath, "GameVersion.json");
        FileInfo fileInfo = new(filePath);

        if (!fileInfo.Exists)
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameManager::LoadConfig] File GameVersion.json doesn't exist on dir: {Dir}",
                CurrentGameInstallPath);
            return;
        }

        try
        {
            using FileStream fileStream = fileInfo.OpenRead();
            GameVersion = JsonSerializer.Deserialize(fileStream, DNAFilesContext.Default.DNAFilesGameVersion);

            CurrentGameVersion = new GameVersion($"{GameVersion?.Version}.0.0");

            SharedStatic.InstanceLogger.LogTrace("[DNAGameManager::LoadConfig] Loaded GameVersion.json ({Version}) from directory: {Dir}",
                GameVersion?.Version, CurrentGameInstallPath);
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameManager::LoadConfig] Cannot load GameVersion.json! Reason: {Exception}", ex);
        }
    }

    public override void SaveConfig()
    {
        // NOP
    }
}