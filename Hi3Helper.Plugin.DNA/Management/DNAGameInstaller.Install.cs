using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA.Management;

internal partial class DNAGameInstaller : GameInstallerBase
{
    protected bool _canSkipDelete => File.Exists(Path.Combine(_gamePath!, "@NoDelete"));
    protected bool _canSkipVerif => File.Exists(Path.Combine(_gamePath!, "@NoVerification"));
    protected bool _canSkipExtract => File.Exists(Path.Combine(_gamePath!, "@NoExtraction"));

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

        var tempJsonPath = Path.Combine(tempPath, _downloadVersionName);
        await WriteDownloadVersionJson(tempJsonPath);

        // 0. Set Initial State
        InstallProgress installProgress = new()
        {
            StateCount = 0,
            TotalStateToComplete = _downloadThreads,
            DownloadedCount = GetDownloadedCountAsync(GameInstallerKind.Install, token),
            TotalCountToDownload = _downloadThreads,
            DownloadedBytes = await GetGameDownloadedSizeAsyncInner(GameInstallerKind.Install, token),
            TotalBytesToDownload = await GetGameSizeAsyncInner(GameInstallerKind.Install, token)
        };

        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Download);

        var fileSize = HdiffInfo.HdiffFile.Size!.Value;

        var segmentFiles = Enumerable.Range(0, _downloadThreads)
            .Select(i => (
            outputName: Path.Combine(tempPath, $"{HdiffInfo.HdiffFile.Name}.seg{i}"),
            offset: i * (fileSize / (_downloadThreads * _blockSize) * _blockSize),
            expectedSize: i == _downloadThreads - 1
            ? fileSize - ((_downloadThreads - 1) * (fileSize / (_downloadThreads * _blockSize) * _blockSize))
            : fileSize / (_downloadThreads * _blockSize) * _blockSize)).ToList();

        var hdiffUrl = _gameManager!.ApiDetails.GetHdiffFileUrl(
            _gameManager!.VersionManifest!.LatestVersionNumber!,
            _gameManager!.VersionManifest!.LatestVersion!,
            HdiffInfo.HdiffFile.Name!,
            _gameVersion);

        // Head request for file (optional but launcher does it)
        var headRequest = new HttpRequestMessage(HttpMethod.Head, hdiffUrl);
        var headResponse = await _downloadHttpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, token);
        headResponse.EnsureSuccessStatusCode();

        int retries = 0;
        while (true)
        {
            // 1. File Download
            await Parallel.ForEachAsync(segmentFiles, new ParallelOptions
            {
                MaxDegreeOfParallelism = _downloadThreads,
                CancellationToken = token
            }, DownloadImpl);

            // 2. File Verification
            installProgress.StateCount = 0;
            installProgress.DownloadedCount = 0;
            installProgress.DownloadedBytes = 0;
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Verify);

            // #### await MergeAndVerify .hdiff ####

            retries++;

            if (retries >= 5)
            {
                throw new Exception("Exceeded amount of allowed retries.");
            }

            if (installProgress.DownloadedCount == _downloadThreads)
                break;

            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Download);
        }

        // 3. Game File Patching
        installProgress.StateCount = 0;
        installProgress.DownloadedCount = 0;
        installProgress.DownloadedBytes = 0;
        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Install);

        // #### await Hdiff extraction ####

        // 4. File Cleanup
        foreach ((var fileName, _, _) in segmentFiles)
        {
            if (_canSkipDelete) break;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        // 5. Finalize installation - Write BaseVersion.json
        var mainJsonPath = Path.Combine(_gamePath!, _gameVersionName);
        await WriteGameVersionJson(mainJsonPath);

        Directory.Delete(tempPath, true);

        _gameManager!.GetApiGameVersion(out GameVersion apiVersion);
        _gameManager!.SetCurrentGameVersion(apiVersion);

        return;

        #region Subroutine - File Download
        async ValueTask DownloadImpl((string outputName, long offset, long expectedSize) t, CancellationToken token)
        {
            var filePath = Path.Combine(tempPath, t.outputName);
            FileInfo fileInfo = new(filePath);
            fileInfo.Directory?.Create();

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
            }

            await using FileStream fileStream = fileInfo.Open(FileMode.Append, FileAccess.Write, FileShare.Write);

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Trying to download {outputName} at offset {Offset}", t.outputName, t.offset);
#endif
            long existingLength = 0;
            if (fileStream.CanSeek)
            {
                existingLength = fileStream.Length;
                fileStream.Seek(existingLength, SeekOrigin.Begin);
            }

#if DEBUG
            if (existingLength >= 0)
            {
                SharedStatic.InstanceLogger.LogTrace("Already downloaded {bytes} bytes of {outputName}", existingLength, t.outputName);
            }
#endif

            // Use Retry-able Copy-To Stream task to start the download
            await using RetryableCopyToStreamTask downloadTask = RetryableCopyToStreamTask
                .CreateTask((pos, thisCtx) =>
                    {
                        // Start from existing file size (or retry position)
                        long startPos = existingLength + pos;
                        long endPos = t.offset + t.expectedSize;

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
                        MaxBufferSize = 8 << 10,
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
            SharedStatic.InstanceLogger.LogTrace("Downloaded {Segment} at offset {Offset}", t.outputName, t.offset);
#endif
        }
        #endregion

        #region Subroutine - File Verification
        async Task VerifyFiles()
        {
            if (_canSkipVerif) return;
            
            Interlocked.Increment(ref installProgress.DownloadedCount);
            Interlocked.Increment(ref installProgress.StateCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Verify);
        }
        #endregion
    }

    private async Task WriteDownloadVersionJson(string jsonPath)
    {
        if (HdiffInfo?.HdiffFile == null)
            return;

        FileInfo fileInfo = new FileInfo(jsonPath);
        fileInfo.Directory?.Create();

        using var output = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);

        var fileVersion = _gameManager?.ApiDetails.GetHdiffFileName(
            _gameManager.VersionManifest!.LatestVersionNumber!,
            _gameManager.VersionManifest!.LatestVersion!,
            _gameVersion);
            
        var downloadVersion = new DNAFilesDownloadVersion()
        {
            Version = fileVersion,
            MD5 = HdiffInfo.HdiffFile.MD5,
            IsFullPatch = _gameVersion != null
        };

        await JsonSerializer.SerializeAsync(output, downloadVersion, DNAFilesContext.Default.DNAFilesDownloadVersion);
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