using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.DNA.Management.Api;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA.Management;

[GeneratedComClass]
internal partial class DNAGameInstaller : GameInstallerBase
{
    private readonly HttpClient _downloadHttpClient = DNAUtility.CreateApiHttpClient();
    private DNAGameManager? _gameManager;

    private string? _gamePath = null;
    private string? _gameVersion = null;

    private const string _tempPath = "TempPath";

    private DNAApiResponseHdiffInfo? HdiffInfo = null;

    private const string _gameVersionName = "GameVersion.json";

    public DNAGameInstaller(IGameManager? gameManager) : base(gameManager)
    {
        _gameManager = gameManager as DNAGameManager;
    }

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        GameManager.GetGamePath(out _gamePath);

        _gameVersion = _gameManager!.GameVersion?.Version.ToString();
        if (_gameVersion != null)
        {
            long gameVer = long.Parse(_gameVersion);
            long minVer = long.Parse(_gameManager!.VersionManifest!.MinSupportedVersion!);

            if (gameVer < minVer)
            {
                _gameVersion = null;
#if DEBUG
                SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::InitAsync] Game version is too old to be updated, downloading full package.");
#endif
            }
        }

        // Get information about file to download
        var hdiffInfoUrl = _gameManager!.ApiDetails.GetHdiffInfoUrl(
            _gameManager.VersionManifest!.LatestVersionNumber!,
            _gameManager.VersionManifest!.LatestVersion!,
            _gameVersion);
        using HttpResponseMessage hdiffInfoResponse = await _downloadHttpClient.GetAsync(hdiffInfoUrl, HttpCompletionOption.ResponseHeadersRead, token);
        hdiffInfoResponse.EnsureSuccessStatusCode();

        string jsonResponse = await hdiffInfoResponse.Content.ReadAsStringAsync(token);
        HdiffInfo = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseHdiffInfo);

        return 0;
    }

    public override void Dispose()
    {
        // NOP
    }

    protected override async Task<long> GetGameDownloadedSizeAsyncInner(GameInstallerKind gameInstallerKind, CancellationToken token)
    {
        if (HdiffInfo == null)
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

        if (HdiffInfo?.HdiffFile == null)
            return 0L;

        var tempPath = Path.Combine(_gamePath, _tempPath);
        var downloadedSize = 0L;

        var filePath = Path.Combine(tempPath, HdiffInfo.HdiffFile.Name!);
        try
        {
            var fi = new FileInfo(filePath);
            downloadedSize += fi.Length;
#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("[DNAGameInstaller::CalculateDownloadedBytesAsync] Counted existing file {File} len={Len}", filePath, fi.Length);
#endif
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::CalculateDownloadedBytesAsync] Error reading file info {File}: {Err}", filePath, ex.Message);
        }

        return downloadedSize;
    }

    protected override async Task<long> GetGameSizeAsyncInner(GameInstallerKind gameInstallerKind, CancellationToken token)
    {
        if (gameInstallerKind is GameInstallerKind.None or GameInstallerKind.Preload)
            return 0;

        if (HdiffInfo?.HdiffFile == null)
        {
            SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::GetGameSizeAsyncInner] No information about the download exist");
            return 0L;
        }

#if DEBUG
        SharedStatic.InstanceLogger.LogWarning("[DNAGameInstaller::GetGameSizeAsyncInner] Download size: {DS} / Final size: {FS}",
            HdiffInfo.HdiffFile.Size, HdiffInfo.NewSize);
#endif

        return HdiffInfo.HdiffFile.Size ?? 0L;
    }

    private int GetDownloadedCountAsync(GameInstallerKind install, CancellationToken token)
    {
        if (install is GameInstallerKind.None or GameInstallerKind.Preload)
            return 0;

        if (HdiffInfo?.HdiffFile == null)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameInstaller::StartInstallAsyncInner] HdiffFile is null, aborting.");
            throw new InvalidOperationException("No information about download package.");
        }

        var file = HdiffInfo.HdiffFile;

        var tempPath = Path.Combine(_gamePath!, _tempPath);
        if (!Directory.Exists(tempPath))
            return 0;

        var totalDownloaded = 0;

        var filePath = Path.Combine(tempPath, HdiffInfo.HdiffFile.Name!);
        if (File.Exists(filePath))
        {
            var fileSize = new FileInfo(filePath).Length;
            if (fileSize == file.Size)
                totalDownloaded++;
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