using System;
using System.Collections.Generic;
using System.Linq;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiDetails
{
    public List<string> BaseUrls { get; init; } = null!;

    public string BaseUrl {
        get
        {
            int index = (int)Math.Floor((DateTime.Now.Day - 1) / 8.0);
            if (index >= BaseUrls.Count) return BaseUrls.First();

            return BaseUrls[index];
        }
    }

    public string Region { get; init; } = null!;
    public string RegionLong { get; init; } = null!;
    public string Tag { get; init; } = null!;

    #region URL Formatting Methods

    #region Asset URLs
    public string GetVersionManifestUrl() => $"{BaseUrl}/Packages/{Region}/WindowsNoEditor/{Tag}/VersionManifest.json ";

    protected string GetDiffTag(string? currentVersion, string latestVersion) => 
        currentVersion == null ? $"full_{latestVersion}" : $"v{currentVersion}_to_v{latestVersion}";
    
    public string GetHdiffInfoUrl(string versionNumber, string latestVersion, string? currentVersion = null)
    {
        var diffTag = GetDiffTag(currentVersion, latestVersion);

        return $"{BaseUrl}/Packages/{Region}/WindowsNoEditor/{Tag}/{versionNumber}/{latestVersion}/{diffTag}/HPatchDiffMd5.json";
    }

    public string GetHdiffFileUrl(string versionNumber, string latestVersion, string fileName, string? currentVersion = null)
    {
        var diffTag = GetDiffTag(currentVersion, latestVersion);

        return $"{BaseUrl}/Packages/{Region}/WindowsNoEditor/{Tag}/{versionNumber}/{latestVersion}/{diffTag}/{fileName}";
    }
    #endregion

    #region Media URLs
    public static string GetBackgroundUrl(string versionNumber) => $"https://cdnstatic.yingxiong.com/dna_hd_{versionNumber.Replace(".","_")}/imgs/home/pc/bg.webp";

    public string GetNoticesUrl() => $"{BaseUrl}/OperationLauncherNotice/OperationLauncherNoticeProduction{RegionLong}online.json";

    public string GetCarouselUrl() => $"{BaseUrl}/OperationLauncherHeadImage/OperationLauncherHeadImageProduction{RegionLong}online.json";

    public string GetSocialsUrl() => $"{BaseUrl}/OperationLauncherSocialMedia/OperationLauncherSocialMediaProduction{RegionLong}online.json";
    #endregion

    #endregion
}
