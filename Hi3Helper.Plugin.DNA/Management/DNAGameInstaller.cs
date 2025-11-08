using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.DNA.Management.Api;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA.Management;

[GeneratedComClass]
internal partial class DNAGameInstaller(IGameManager? gameManager, string apiResponseBaseUrl) : GameInstallerBase(gameManager)
{
    private HttpClient _downloadHttpClient = DNAUtility.CreateApiHttpClient();
    private DNAGameManager? _gameManager = gameManager as DNAGameManager;

    private string? _baseVersionUrl = apiResponseBaseUrl + "/Packages/Global/WindowsNoEditor/PC_OBT_Global_Pub/";
    private const string _baseVersion = "BaseVersion.json";

    private string? _gamePath = null;
    
    private DNAApiResponseVersion? _apiVersion;
    private DNAFilesVersion? _installVersion;
    private DNAApiResponseVersion? _tempVersion;

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        GameManager.GetGamePath(out _gamePath);

        // Attempt to download API BaseVersion
        try
        {
            using HttpResponseMessage versionResponse =
            await _downloadHttpClient.GetAsync(_baseVersionUrl + _baseVersion, HttpCompletionOption.ResponseHeadersRead, token);
            versionResponse.EnsureSuccessStatusCode();

            string jsonResponse = await versionResponse.Content.ReadAsStringAsync(token);
            _apiVersion = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseVersion);
            SharedStatic.InstanceLogger.LogDebug("[DNAGameInstaller::InitAsync] Downloaded BaseVersion.json");
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::InitAsync] Failed to get API BaseVersion.json: {Err}", ex.Message);
            _apiVersion = null;
        }

        // Attempt to parse Install BaseVersion
        try
        {
            var jsonPath = Path.Combine(_gamePath!, _baseVersion);
            if (Path.Exists(jsonPath))
            {
                using var jsonResponse = File.OpenRead(jsonPath);
                _installVersion = JsonSerializer.Deserialize(jsonResponse, DNAFilesContext.Default.DNAFilesVersion);
                SharedStatic.InstanceLogger.LogDebug("[DNAGameInstaller::InitAsync] Deserialized Install BaseVersion.json");
            }
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::InitAsync] Failed to deserialize Install BaseVersion: {Err}", ex.Message);
            _installVersion = null;
        }

        // Attempt to parse Temp BaseVersion 
        try
        {
            var jsonPath = Path.Combine(_gamePath!, "TempPath", _baseVersion);
            if (Path.Exists(jsonPath))
            {
                using var jsonResponse = File.OpenRead(jsonPath);
                _tempVersion = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseVersion);
                SharedStatic.InstanceLogger.LogDebug("[DNAGameInstaller::InitAsync] Deserialized Temp BaseVersion.json");
            }
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::InitAsync] Failed to deserialize Temp BaseVersion: {Err}", ex.Message);
            _tempVersion = null;
        }

        return 0;
    }

    public override void Dispose()
    {
        // NOP
    }

    protected override async Task<long> GetGameDownloadedSizeAsyncInner(GameInstallerKind gameInstallerKind, CancellationToken token)
    {
        if (_apiVersion == null)
            return 0L;

        await InitAsync(token).ConfigureAwait(false);

        return gameInstallerKind switch
        {
            GameInstallerKind.None or GameInstallerKind.Preload => 0L,
            GameInstallerKind.Install or GameInstallerKind.Update =>
                CalculateDownloadedBytesAsync(token),
            _ => 0L,
        };
    }

    private long CalculateDownloadedBytesAsync(CancellationToken token)
    {
        if (string.IsNullOrEmpty(_gamePath))
            return 0L;

        if (_apiVersion == null)
            return 0L;

        (var missingFiles, var filesToDelete) = VersionUtils.FindMissingFiles(_apiVersion.FilesList, _installVersion?.FilesList, _tempVersion?.FilesList);

        var filteredFiles = missingFiles.Where(x => !filesToDelete.Contains(x.Key));

        // Check main files
        var tempPath = Path.Combine(_gamePath, "TempPath", "TempGameFiles");
        var downloadedSize = 0L;
        foreach (var files in filteredFiles)
        {
            var filePath = Path.Combine(tempPath, files.Key);

            try
            {
                var fi = new FileInfo(filePath);
                downloadedSize += fi.Length;
                SharedStatic.InstanceLogger.LogTrace("[DNAGameInstaller::CalculateDownloadedBytesAsync] Counted existing file {File} len={Len}", filePath, fi.Length);
            }
            catch (Exception ex)
            {
                SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::CalculateDownloadedBytesAsync] Error reading file info {File}: {Err}", filePath, ex.Message);
            }
        }

        return downloadedSize;
    }

    protected override async Task<long> GetGameSizeAsyncInner(GameInstallerKind gameInstallerKind, CancellationToken token)
    {
        if (gameInstallerKind is GameInstallerKind.None or GameInstallerKind.Preload)
            return 0;

        if (_apiVersion == null || _apiVersion.GameVersionList.Count == 0)
        {
            SharedStatic.InstanceLogger.LogDebug("[DNAGameInstaller::GetGameSizeAsyncInner] Version list empty or null");
            return 0L;
        }

        (var missingFiles, _) = VersionUtils.FindMissingFiles(_apiVersion.FilesList, _installVersion?.FilesList, _tempVersion?.FilesList);

        try
        {
            long total = 0;
            foreach ((_, var file) in missingFiles)
            {
                total = unchecked(total + file.Size);
            }

            SharedStatic.InstanceLogger.LogDebug("[DNAGameInstaller::GetGameSizeAsyncInner] Computed total size: {Total}", total);
            return total;
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogWarning("[WuwaGameInstaller::GetGameSizeAsyncInner] Error computing total size: {Err}", ex.Message);
            return 0L;
        }
    }

    private int GetDownloadedCountAsync(GameInstallerKind install, CancellationToken token)
    {
        if (install is GameInstallerKind.None or GameInstallerKind.Preload)
            return 0;

        if (_apiVersion is null)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameInstaller::StartInstallAsyncInner] _apiVersion is null, aborting.");
            throw new InvalidOperationException("BaseVersion from API is not initialized.");
        }

        (var missingFiles, _) = VersionUtils.FindMissingFiles(_apiVersion.FilesList, _installVersion?.FilesList, _tempVersion?.FilesList);

        var tempPath = Path.Combine(_gamePath!, "TempPath", "TempGameFiles");
        if (!Directory.Exists(tempPath))
            return 0;

        var totalDownloaded = 0;
        foreach ((var fileName, var fileDetails) in missingFiles)
        {
            var filePath = Path.Combine(tempPath, fileName);
            if (File.Exists(filePath))
            {
                var existingSize = new FileInfo(filePath).Length;
                if (existingSize == fileDetails.Size)
                    totalDownloaded++;
            }
        }

        return totalDownloaded;
    }

    protected override async Task UninstallAsyncInner(CancellationToken token)
    {
        GameManager.IsGameInstalled(out bool isInstalled);
        if (!isInstalled)
            return;

        GameManager.GetGamePath(out string? installPath);
        if (string.IsNullOrEmpty(installPath))
            return;

        await Task.Run(() => Directory.Delete(installPath, true), token).ConfigureAwait(false);
    }
}