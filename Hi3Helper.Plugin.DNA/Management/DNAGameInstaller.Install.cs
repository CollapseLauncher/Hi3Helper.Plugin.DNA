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

        (var missingFiles, var filesToDelete) = VersionUtils.FindMissingFiles(_apiVersion.FilesList, _installVersion?.FilesList, _tempVersion?.FilesList);

        var tempPath = Path.Combine(_gamePath!, "TempPath", "TempGameFiles");
        Directory.CreateDirectory(tempPath);

        var tempJsonPath = Path.Combine(_gamePath!, "TempPath", _baseVersion);
        await WriteVersionJson(tempJsonPath);

        // 1. File Download
        InstallProgress installProgress = new()
        {
            StateCount = 0,
            TotalStateToComplete = missingFiles.Count,
            DownloadedCount = GetDownloadedCountAsync(GameInstallerKind.Install, token),
            TotalCountToDownload = missingFiles.Count,
            DownloadedBytes = await GetGameDownloadedSizeAsyncInner(GameInstallerKind.Install, token),
            TotalBytesToDownload = await GetGameSizeAsyncInner(GameInstallerKind.Install, token)
        };

        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Download);

        await Parallel.ForEachAsync(missingFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        }, DownloadImpl);

        // 2. File Verification
        installProgress.StateCount = 0;
        installProgress.DownloadedCount = 0;
        installProgress.DownloadedBytes = 0;
        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Verify);

        await Parallel.ForEachAsync(missingFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        }, VerifyImpl);

        // 3. File Extraction
        installProgress.StateCount = 0;
        installProgress.DownloadedCount = 0;
        installProgress.DownloadedBytes = 0;
        progressDelegate?.Invoke(in installProgress);
        progressStateDelegate?.Invoke(InstallProgressState.Install);

        await Parallel.ForEachAsync(missingFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = token
        }, ExtractImpl);

        // 4. File Cleanup
        foreach ((var fileName, _) in missingFiles)
        {
            if (_canSkipDeleteZip) break;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        // 5. Finalize installation - Write BaseVersion.json
        var mainJsonPath = Path.Combine(_gamePath!, _baseVersion);
        await WriteVersionJson(mainJsonPath, true);

        Directory.Delete(tempPath, true);

        _gameManager?.SetCurrentGameVersion(_apiVersion.ToFile());

        return;

        #region Subroutine - File Download
        async ValueTask DownloadImpl(KeyValuePair<string, DNAApiResponseVersionFileInfo> file, CancellationToken innerToken)
        {
            (string fileName, DNAApiResponseVersionFileInfo fileDetails) = file;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new(filePath);
            fileInfo.Directory?.Create();

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
                if (filesToDelete.Contains(fileName))
                {
                    fileInfo.Delete();
                }
            }

            await using FileStream fileStream = fileInfo.Open(FileMode.Append, FileAccess.Write, FileShare.Write);

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
            }

            if (existingLength >= fileDetails.Size)
            {
#if DEBUG
                SharedStatic.InstanceLogger.LogTrace("Already downloaded asset from URL: {AssetUrl}", assetDownloadUrl);
#endif
                Interlocked.Increment(ref installProgress.StateCount);
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

            Interlocked.Increment(ref installProgress.StateCount);
            Interlocked.Increment(ref installProgress.DownloadedCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Download);

#if DEBUG
            SharedStatic.InstanceLogger.LogTrace("Downloaded asset from URL: {AssetUrl}", assetDownloadUrl);
#endif
        }
#endregion

        #region Subroutine - File Verification
        async ValueTask VerifyImpl(KeyValuePair<string, DNAApiResponseVersionFileInfo> file, CancellationToken innerToken)
        {
            if (_canSkipVerif) return;

            (string fileName, DNAApiResponseVersionFileInfo fileDetails) = file;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new(filePath);
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
                var callback = (long e) =>
                {
                    Interlocked.Add(ref installProgress.DownloadedBytes, e);
                    progressDelegate?.Invoke(in installProgress);
                    progressStateDelegate?.Invoke(InstallProgressState.Verify);
                };

                var fs = File.OpenRead(filePath);
                var checksumMd5 = (await HashUtils.ComputeMd5HexAsync(fs, callback, token))?.Trim()?.ToLowerInvariant();
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
            Interlocked.Increment(ref installProgress.StateCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Verify);
        }
        #endregion

        #region Subroutine - File Extraction
        async ValueTask ExtractImpl(KeyValuePair<string, DNAApiResponseVersionFileInfo> file, CancellationToken innerToken)
        {
            if (_canSkipExtract) return;

            (string fileName, DNAApiResponseVersionFileInfo fileDetails) = file;

            var filePath = Path.Combine(tempPath, fileName);
            FileInfo fileInfo = new(filePath);

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
                Interlocked.Exchange(ref installProgress.TotalBytesToDownload, (long)e.TotalSize);
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
            Interlocked.Increment(ref installProgress.StateCount);
            progressDelegate?.Invoke(in installProgress);
            progressStateDelegate?.Invoke(InstallProgressState.Install);
        }
        #endregion
    }

    private async Task WriteVersionJson(string jsonPath, bool isFile = false)
    {
        if (_apiVersion == null)
            return;

        FileInfo fileInfo = new FileInfo(jsonPath);
        fileInfo.Directory?.Create();

        using var output = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None);

        if (isFile)
        {
            var installVersion = _apiVersion.ToFile();
            await JsonSerializer.SerializeAsync(output, installVersion, DNAFilesContext.Default.DNAFilesVersion);
        }
        else
        {
            await JsonSerializer.SerializeAsync(output, _apiVersion, DNAApiResponseContext.Default.DNAApiResponseVersion);
        }  
    }

    protected override Task StartUpdateAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        // Updates are the exact same as installs, but BaseVersions already exists! °□°
        return StartInstallAsyncInner(progressDelegate, progressStateDelegate, token);
    }

    protected override Task StartPreloadAsyncInner(InstallProgressDelegate? progressDelegate, InstallProgressStateDelegate? progressStateDelegate, CancellationToken token)
    {
        // NOP
        return Task.CompletedTask;
    }
}