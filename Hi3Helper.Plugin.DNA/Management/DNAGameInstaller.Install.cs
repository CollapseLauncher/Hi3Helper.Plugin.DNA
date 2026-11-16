using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using SharpHPatchZ;
using SharpHPatchZ.Header;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA.Management;

internal partial class DNAGameInstaller : GameInstallerBase
{
    protected bool _canSkipDelete => File.Exists(Path.Combine(_gamePath!, "@NoDelete"));
    protected bool _canSkipVerif => File.Exists(Path.Combine(_gamePath!, "@NoVerification"));
    protected bool _canSkipPatch => File.Exists(Path.Combine(_gamePath!, "@NoPatch"));

    protected override async Task StartInstallAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        SharedStatic.InstanceLogger.LogInformation("[DNAGameInstaller::StartInstallAsyncInner] Starting installation routine.");

        if (HdiffInfo?.HdiffFile is null)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameInstaller::StartInstallAsyncInner] File information is null, aborting.");
            throw new InvalidOperationException("No information about download package.");
        }

        await InitAsync(token).ConfigureAwait(false);

        if (_gamePath is null)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameInstaller::StartInstallAsyncInner] _gamePath is null, aborting.");
            throw new InvalidOperationException("Game location was not set before installation.");
        }

        var tempPath = Path.Combine(_gamePath!, _tempPath);
        Directory.CreateDirectory(tempPath);

        var fileSize = HdiffInfo.HdiffFile.Size!.Value;
        var hdiffName = HdiffInfo.HdiffFile.Name!;

        var hdiffUrl = _gameManager!.ApiDetails.GetHdiffFileUrl(
            _gameManager!.VersionManifest!.LatestVersionNumber!,
            _gameManager!.VersionManifest!.LatestVersion!,
            hdiffName,
            _gameVersion);

        
        var hdiffPath = Path.Combine(tempPath, hdiffName);


        // Head request for file (optional but launcher does it)
        var headRequest = new HttpRequestMessage(HttpMethod.Head, hdiffUrl);
        var headResponse = await _downloadHttpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, token);
        headResponse.EnsureSuccessStatusCode();


        InstallProgress installProgress = new();

        int retries = 0;
        while (true)
        {
            // 0. Set Initial State
            installProgress = new()
            {
                StateCount = 0,
                TotalStateToComplete = 1,
                DownloadedCount = GetDownloadedCountAsync(GameInstallerKind.Install, token),
                TotalCountToDownload = 1,
                DownloadedBytes = await GetGameDownloadedSizeAsyncInner(GameInstallerKind.Install, token),
                TotalBytesToDownload = await GetGameSizeAsyncInner(GameInstallerKind.Install, token)
            };
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Download);

            // 1. File Download
            await DownloadFile();

            // 2. File Verification
            await VerifyFile();

            if (installProgress.DownloadedCount == 1)
                break;

            retries++;
            if (retries >= 5)
                throw new Exception("Exceeded amount of allowed retries.");
        }

        // 4. Game File Patching
        await PatchFiles();

        // 5. File Cleanup
        if (!_canSkipDelete)
        {
            var fileInfo = new FileInfo(hdiffPath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            Directory.Delete(tempPath, true);
        }

        // 6. Finalize installation - Write GameVersion.json
        var mainJsonPath = Path.Combine(_gamePath!, _gameVersionName);
        await WriteGameVersionJson(mainJsonPath);

        _gameManager!.GetApiGameVersion(out GameVersion apiVersion);
        _gameManager!.SetCurrentGameVersion(apiVersion);

        return;

        #region Subroutine - File Download
        async Task DownloadFile()
        {
            FileInfo fileInfo = new(hdiffPath);
            fileInfo.Directory?.Create();

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
            }

            await using FileStream fileStream = fileInfo.Open(FileMode.Append, FileAccess.Write, FileShare.Write);

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Trying to download {outputName}", hdiffName);
#endif
            long existingLength = 0;
            if (fileStream.CanSeek)
            {
                existingLength = fileStream.Length;
                fileStream.Seek(existingLength, SeekOrigin.Begin);
            }

            if (existingLength == fileSize)
            {
                SharedStatic.InstanceLogger.LogInformation("Already downloaded all {bytes} bytes of {outputName}", existingLength, hdiffName);
                return;
            }

#if DEBUG
            if (existingLength >= 0)
            {
                SharedStatic.InstanceLogger.LogTrace("Already downloaded {bytes} bytes of {outputName}", existingLength, hdiffName);
            }
#endif

            // Use Retry-able Copy-To Stream task to start the download
            await using RetryableCopyToStreamTask downloadTask = RetryableCopyToStreamTask
                .CreateTask((pos, thisCtx) =>
                    {
                        // Start from existing file size (or retry position)
                        long startPos = existingLength + pos;
                        long endPos = fileSize - 1;

                        return _downloadHttpClient.CreateHttpBridgedStream(
                            hdiffUrl,
                            startPos,
                            endPos,
                            thisCtx);
                    },
                    fileStream,
                    new RetryableCopyToStreamTaskOptions
                    {
                        IsDisposeTargetStream = true,
                        MaxBufferSize = 128 << 10,
                        RetryDelaySeconds = 1d
                    });

            // Start download task
            await downloadTask.StartTaskAsync(read =>
            {
                Interlocked.Add(ref installProgress.DownloadedBytes, read);
                progressDelegate?.Invoke(in installProgress);
            }, token);

            Interlocked.Increment(ref installProgress.StateCount);
            Interlocked.Increment(ref installProgress.DownloadedCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Download);

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Downloaded {Segment}", hdiffName);
#endif
        }
        #endregion

        #region Subroutine - File Verification

        async Task VerifyFile()
        {
            if (_canSkipVerif) return;

            installProgress.StateCount = 0;
            installProgress.TotalStateToComplete = 1;
            installProgress.DownloadedCount = 0;
            installProgress.DownloadedBytes = 0;
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Verify);

            using var input = File.OpenRead(hdiffPath);
            var fileMd5 = await HashUtils.ComputeMd5HexAsync(input, (o) =>
            {
                Interlocked.Add(ref installProgress.DownloadedBytes, o);
                progressDelegate?.Invoke(in installProgress);
            }, token);

            if (fileMd5 != null && fileMd5 != HdiffInfo.HdiffFile.MD5)
            {
                throw new Exception("File hash doesn't match VersionManifest");
            }
            
            Interlocked.Increment(ref installProgress.DownloadedCount);
            Interlocked.Increment(ref installProgress.StateCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Verify);
        }
        #endregion

        #region Subroutine - Patch Files
        async Task PatchFiles()
        {
            if (_canSkipPatch)
                return;

            installProgress.StateCount = 0;
            installProgress.TotalStateToComplete = 1;
            installProgress.DownloadedCount = 0;
            installProgress.DownloadedBytes = 0;
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Install);

            using HDiffInfo info = HPatch.CreateInstance(hdiffPath);
           
            PatchResult patchResult = HPatch.Patch(info, _tempPath, _gamePath, _gamePath, progressCallback: UpdateProgress, token: token);

            if (!patchResult)
                throw patchResult.Exception ?? new Exception();

            installProgress.StateCount = 1;
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Install);

            return;

            void UpdateProgress(long totalProcessed, long totalSize, int written)
            {
                Interlocked.Exchange(ref installProgress.DownloadedBytes, totalProcessed);
                Interlocked.Exchange(ref installProgress.TotalBytesToDownload, totalSize);
                progressDelegate?.Invoke(in installProgress);
            }
        }
        #endregion
    }

    private async Task WriteGameVersionJson(string jsonPath)
    {
        if (HdiffInfo?.HdiffFile == null)
            return;

        FileInfo fileInfo = new FileInfo(jsonPath);
        fileInfo.Directory?.Create();

        using var output = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);

        var gameVersion = new DNAFilesGameVersion()
        {
            Version = _gameManager!.VersionManifest!.LatestVersion!
        };

        await JsonSerializer.SerializeAsync(output, gameVersion, DNAFilesContext.Default.DNAFilesGameVersion);
    }

    protected override Task StartUpdateAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        // Updates are the exact same as installs, but _gameManager.GameVersion is not null
        return StartInstallAsyncInner(progressDelegate, progressStateDelegate, token);
    }

    protected override Task StartPreloadAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        // NOP
        return Task.CompletedTask;
    }
}