using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Management.PresetConfig;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.Api;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using Hi3Helper.Plugin.DNA.Management.PresetConfig;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        string apiResponseBaseUrl,
        DNAPresetConfig preset)
    {
        CurrentGameExecutableByPreset = gameExecutableNameByPreset;
        ApiResponseBaseUrl = apiResponseBaseUrl;
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

    protected override string ApiResponseBaseUrl { get; }

    private string CurrentGameExecutableByPreset { get; }

    private DNAPresetConfig Preset { get; }

    internal string? GameResourceBaseUrl { get; set; }
    internal string? GameResourceBasisPath { get; set; }
    internal bool IsInitialized { get; set; }

    protected override bool HasPreload => false;
    protected override bool HasUpdate => IsInstalled && VersionUtils.CheckUpdate(ApiVersion?.FilesList, InstalledVersion?.FilesList);

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

    private DNAApiResponseVersion? ApiVersion;
    private DNAFilesVersion? InstalledVersion;

    internal async Task<int> InitAsyncInner(bool forceInit = false, CancellationToken token = default)
    {
        if (!forceInit && IsInitialized)
            return 0;

        var apiUrl = ApiResponseBaseUrl + "/Packages/Global/WindowsNoEditor/PC_OBT_Global_Pub/BaseVersion.json";

        using HttpResponseMessage versionResponse =
            await ApiResponseHttpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead, token);
        versionResponse.EnsureSuccessStatusCode();

        string jsonResponse = await versionResponse.Content.ReadAsStringAsync();
        ApiVersion = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseVersion);
        ApiGameVersion = new GameVersion(ApiVersion?.GameVersionList.First().Key);

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
            SharedStatic.InstanceLogger.LogWarning(
                "[DNAGameManager::LoadConfig] Game directory isn't set! Game config won't be loaded.");
            return;
        }

        string filePath = Path.Combine(CurrentGameInstallPath, "BaseVersion.json");
        FileInfo fileInfo = new(filePath);

        if (!fileInfo.Exists)
        {
            SharedStatic.InstanceLogger.LogWarning(
                "[DNAGameManager::LoadConfig] File BaseVersion.json doesn't exist on dir: {Dir}",
                CurrentGameInstallPath);
            return;
        }

        try
        {
            using FileStream fileStream = fileInfo.OpenRead();
            InstalledVersion = JsonSerializer.Deserialize(fileStream, DNAFilesContext.Default.DNAFilesVersion);
            CurrentGameVersion = new GameVersion(InstalledVersion?.GameVersionList.First().Key);

            SharedStatic.InstanceLogger.LogTrace(
                "[DNAGameManager::LoadConfig] Loaded BaseVersion.json from directory: {Dir}",
                CurrentGameInstallPath);
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogError(
                "[DNAGameManager::LoadConfig] Cannot load BaseVersion.json! Reason: {Exception}", ex);
        }
    }

    public override void SaveConfig()
    {
        // NOP
    }
}