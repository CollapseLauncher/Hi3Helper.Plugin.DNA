// ReSharper disable InconsistentNaming

using Hi3Helper.Plugin.DNA.Management.FileStructs;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseVersion
{
    [JsonPropertyName("GameVersionList")]
    public Dictionary<string, DNAApiResponseVersionContainer> GameVersionList { get; set; } = [];

    public DNAFilesVersion ToFile()
    {
        var ret = new DNAFilesVersion();
        foreach ((var version, var container) in GameVersionList)
        {
            ret.GameVersionList.Add(version, container.ToFile());
        }
        return ret;
    }

    [JsonIgnore]
    public Dictionary<string, DNAApiResponseVersionFileInfo> FilesList
        => GameVersionList.FirstOrDefault().Value.FilesList;
}

public class DNAApiResponseVersionContainer
{
    [JsonPropertyName("GameVersionList")]
    public Dictionary<string, DNAApiResponseVersionFileInfo> FilesList { get; set; } = [];

    public DNAFilesVersionContainer ToFile()
    {
        var ret = new DNAFilesVersionContainer();
        foreach ((var version, var file) in FilesList)
        {
            ret.FilesList.Add(version, file.ToFile());
        }
        return ret;
    }
}

public class DNAApiResponseVersionFileInfo
{
    [JsonPropertyName("ZipGameVersion")]
    public int Version { get; set; }

    [JsonPropertyName("ZipMd5")]
    public string? ChecksumMD5 { get; set; }

    [JsonPropertyName("ZipSize")]
    public long Size { get; set; }

    [JsonPropertyName("bIsSDK")]
    public string? IsSDK { get; set; }

    public DNAFilesVersionFileInfo ToFile()
    {
        return new DNAFilesVersionFileInfo
        {
            Version = Version,
            ChecksumMD5 = ChecksumMD5,
            Size = Size,
            IsSDK = IsSDK == bool.TrueString
        };
    }
}
