using Hi3Helper.Plugin.DNA.Management.Api;
using Hi3Helper.Plugin.DNA.Management.FileStructs;
using System.Collections.Generic;

namespace Hi3Helper.Plugin.DNA.Utility;

using ApiFilesDict = Dictionary<string, DNAApiResponseVersionFileInfo>;
using FilesDict = Dictionary<string, DNAFilesVersionFileInfo>;

internal class VersionUtils
{
    internal static bool CheckUpdate(ApiFilesDict? apiVersion, FilesDict? installedVersion)
        => FindMissingFiles(apiVersion, installedVersion, null).Item1.Count != 0;

    internal static (ApiFilesDict, HashSet<string>) FindMissingFiles(ApiFilesDict? apiVersion, FilesDict? installedVersion, ApiFilesDict? tempVersion)
    {
        var diffVersion = new ApiFilesDict();
        var expiredFiles = new HashSet<string>();

        if (apiVersion == null)
            return (diffVersion, expiredFiles);

        foreach ((var fileName, var apiDetails) in apiVersion)
        {
            var tempDetails = tempVersion?.GetValueOrDefault(fileName);
            var installedDetails = installedVersion?.GetValueOrDefault(fileName);

            bool isInstallChange = installedDetails != null && apiDetails.Version != installedDetails.Version;
            bool isNew = installedDetails == null;

            // If file version is not installed or has an update
            if (isInstallChange || isNew)
            {
                diffVersion.TryAdd(fileName, apiDetails);
            }

            // If version changed, queue file for deletion
            if (tempDetails != null && apiDetails.Version != tempDetails.Version)
            {
                expiredFiles.Add(fileName);
            }
        }

        return (diffVersion, expiredFiles);
    }
}
