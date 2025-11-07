using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.Api;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using SevenZipExtractor;
using SevenZipExtractor.Event;
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
    protected bool _canSkipDeleteZip => File.Exists(Path.Combine(_gamePath!, "@NoDeleteZip"));
    protected bool _canSkipVerif => File.Exists(Path.Combine(_gamePath!, "@NoVerification"));
    protected bool _canSkipExtract => File.Exists(Path.Combine(_gamePath!, "@NoExtraction"));

    protected override async Task StartInstallAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        SharedStatic.InstanceLogger.LogInformation("[DNAGameInstaller::StartInstallAsyncInner] Starting installation routine.");

        if (_apiVersion is null)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameInstaller::StartInstallAsyncInner] _apiVersion is null, aborting.");
            throw new InvalidOperationException("BaseVersion from API is not initialized.");
        }

        await InitAsync(token).ConfigureAwait(false);

        if (_gamePath is null)
        {
            SharedStatic.InstanceLogger.LogError("[DNAGameInstaller::StartInstallAsyncInner] _gamePath is null, aborting.");
            throw new InvalidOperationException("Game Path is not initialized.");
        }

        var currentGameVersion = _apiVersion.GameVersionList.Max();
        var tempPath = Path.Combine(_gamePath!, "TempPath", "TempGameFiles");
        Directory.CreateDirectory(tempPath);

        var pendingDownloadedVersion = _gameVersion?.GameVersionList.Max().Key;
        if (pendingDownloadedVersion != null && pendingDownloadedVersion != currentGameVersion.Key)
        {
            try
            {
                Directory.Delete(tempPath, true);
            }
            catch
            {
                // Ignored
            }
        }

        var tempJsonPath = Path.Combine(_gamePath!, "TempPath", _baseVersion);
        await WriteVersionJson(tempJsonPath);

        InstallProgress installProgress = new InstallProgress
        {
            StateCount = 1,
            TotalStateToComplete = 3,
            DownloadedCount = GetDownloadedCountAsync(GameInstallerKind.Install, token),
            TotalCountToDownload = currentGameVersion.Key.Length,
            DownloadedBytes = await GetGameDownloadedSizeAsyncInner(GameInstallerKind.Install, token),
            TotalBytesToDownload = await GetGameSizeAsyncInner(GameInstallerKind.Install, token)
        };

        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Download);

        await Parallel.ForEachAsync(currentGameVersion.Value.FilesList, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        }, DownloadImpl);

        installProgress.StateCount = 2;
        installProgress.DownloadedCount = 0;
        installProgress.DownloadedBytes = 0;
        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Verify);

        await Parallel.ForEachAsync(currentGameVersion.Value.FilesList, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        }, VerifyImpl);

        installProgress.StateCount = 3;
        installProgress.DownloadedCount = 0;
        installProgress.DownloadedBytes = 0;
        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Install);

        await Parallel.ForEachAsync(currentGameVersion.Value.FilesList, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        }, ExtractImpl);

        foreach ((var fileName, _) in currentGameVersion.Value.FilesList)
        {
            if (_canSkipDeleteZip) break;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        // Move BaseVersion.json
        if (File.Exists(tempJsonPath))
        {
            var mainJsonPath = Path.Combine(_gamePath!, _baseVersion);
            File.Move(tempJsonPath, mainJsonPath);
        }

        return;

        async ValueTask DownloadImpl(KeyValuePair<string, DNAApiResponseVersionFileInfo> file, CancellationToken innerToken)
        {
            (string fileName, DNAApiResponseVersionFileInfo fileDetails) = file;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new FileInfo(filePath);
            fileInfo.Directory?.Create();

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
            }

            await using FileStream fileStream = fileInfo.Open(new FileStreamOptions
            {
                Mode = FileMode.Append,
                Access = FileAccess.Write,
                Share = FileShare.Write,
                Options = FileOptions.SequentialScan
            });

            string assetDownloadUrl = _baseVersionUrl + fileName;

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Trying to download the asset from URL: {AssetUrl}", assetDownloadUrl);
#endif
            // Head request for file (optional but launcher does it)
            var headRequest = new HttpRequestMessage(HttpMethod.Head, assetDownloadUrl);
            var headResponse = await _downloadHttpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, token);
            headResponse.EnsureSuccessStatusCode();

            long existingLength = 0;
            if (fileStream.CanSeek)
            {
                existingLength = fileStream.Length;
                fileStream.Seek(existingLength, SeekOrigin.Begin);

                Interlocked.Add(ref installProgress.DownloadedBytes, existingLength);
                progressDelegate?.Invoke(in installProgress);
                progressStateDelegate?.Invoke(InstallProgressState.Download);
            }

            if (existingLength >= fileDetails.Size)
            {
#if DEBUG
                SharedStatic.InstanceLogger.LogTrace("Already downloaded asset from URL: {AssetUrl}", assetDownloadUrl);
#endif
                Interlocked.Increment(ref installProgress.DownloadedCount);
                progressDelegate?.Invoke(in installProgress);
                progressStateDelegate?.Invoke(InstallProgressState.Download);
                return;
            }

            // Use Retry-able Copy-To Stream task to start the download
            await using RetryableCopyToStreamTask downloadTask = RetryableCopyToStreamTask
                .CreateTask((pos, thisCtx) =>
                    {
                        // Start from existing file size (or retry position)
                        long startPos = existingLength + pos;
                        return _downloadHttpClient.CreateHttpBridgedStream(
                            assetDownloadUrl,
                            startPos,
                            null,
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
            }, innerToken);

            Interlocked.Increment(ref installProgress.DownloadedCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Download);

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Downloaded asset from URL: {AssetUrl}", assetDownloadUrl);
#endif
        }

        async ValueTask VerifyImpl(KeyValuePair<string, DNAApiResponseVersionFileInfo> file, CancellationToken innerToken)
        {
            if (_canSkipVerif) return;

            (string fileName, DNAApiResponseVersionFileInfo fileDetails) = file;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new FileInfo(filePath);
            fileInfo.Directory?.Create();

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
            }

            await using FileStream fileStream = fileInfo.Open(new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            });

            var expectedSize = fileDetails.Size;
            var expectedMd5 = fileDetails.ChecksumMD5?.Trim()?.ToLowerInvariant();

            var downloadedSize = new FileInfo(filePath).Length;
            if (downloadedSize != expectedSize)
            {
                throw new IOException($"[DNAGameInstaller::StartInstallAsyncInner] File size for {fileName} does not match ({downloadedSize} vs {expectedSize}).");
            }

            if (expectedMd5 != null)
            {
                var fs = File.OpenRead(filePath);
                var checksumMd5 = (await DNAUtility.ComputeMd5HexAsync(fs, token))?.Trim()?.ToLowerInvariant();
                await fs.DisposeAsync();

                if (checksumMd5 != expectedMd5)
                {
                    throw new IOException($"[DNAGameInstaller::StartInstallAsyncInner] MD5 for {fileName} does not match. ({checksumMd5} vs {expectedMd5})");
                }
            }
#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("[DNAGameInstaller::StartInstallAsyncInner] {file} has been downloaded and checksum matches or is null.", fileName);
#endif
            Interlocked.Increment(ref installProgress.DownloadedCount);
            Interlocked.Add(ref installProgress.DownloadedBytes, fileInfo.Length);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Verify);
        }

        async ValueTask ExtractImpl(KeyValuePair<string, DNAApiResponseVersionFileInfo> file, CancellationToken innerToken)
        {
            if (_canSkipExtract) return;

            (string fileName, DNAApiResponseVersionFileInfo fileDetails) = file;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new FileInfo(filePath);

            await using FileStream fileStream = fileInfo.Open(new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan
            });

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Trying to extract the ZIP archive: {ZipName}", filePath);
#endif

            ArchiveFile? archiveFile = null;

            void ZipProgressAdapter(object? sender, ExtractProgressProp e)
            {
                Interlocked.Add(ref installProgress.DownloadedBytes, (long)e.Read);
                progressDelegate?.Invoke(in installProgress);
                progressStateDelegate?.Invoke(InstallProgressState.Install);
            }

            try
            {
                archiveFile = ArchiveFile.Create(fileStream, true);

                // Start extraction
                archiveFile.ExtractProgress += ZipProgressAdapter;
                await archiveFile.ExtractAsync(e => Path.Combine(_gamePath, e!.FileName!), true, 1 << 20, token);
            }
            finally
            {
                if (archiveFile != null)
                {
                    archiveFile.ExtractProgress -= ZipProgressAdapter;
                    archiveFile.Dispose();
                }
            }

            Interlocked.Increment(ref installProgress.DownloadedCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Install);
        }
    }

    private async Task WriteVersionJson(string jsonPath)
    {
        if (_apiVersion == null)
            return;

        FileInfo fileInfo = new FileInfo(jsonPath);
        if (!File.Exists(jsonPath) || _gameVersion == null)
        {
            _gameVersion = _apiVersion.ToFile();
            fileInfo.Directory?.Create();

            using var output = File.OpenWrite(jsonPath);
            await JsonSerializer.SerializeAsync(output, _gameVersion, DNAFilesContext.Default.DNAFilesVersion);
        }
    }

    protected override Task StartPreloadAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        // NOP
        return Task.CompletedTask;
    }

    protected override Task StartUpdateAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        // NOP
        return Task.CompletedTask;
    }
}