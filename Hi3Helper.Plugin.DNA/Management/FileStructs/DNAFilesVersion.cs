// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.FileStructs;

public class DNAFilesVersion
{
    [JsonPropertyName("gameVersionList")]
    public Dictionary<string, DNAFilesVersionContainer> GameVersionList { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, DNAFilesVersionFileInfo>? FilesList
        => GameVersionList?.FirstOrDefault().Value.FilesList;
}

public class DNAFilesVersionContainer
{
    [JsonPropertyName("gameVersionList")]
    public Dictionary<string, DNAFilesVersionFileInfo> FilesList { get; set; } = [];
}

public class DNAFilesVersionFileInfo
{
    [JsonPropertyName("zipGameVersion")]
    public int Version { get; set; }

    [JsonPropertyName("zipMd5")]
    public string? ChecksumMD5 { get; set; }

    [JsonPropertyName("zipSize")]
    public long Size { get; set; }

    [JsonPropertyName("bIsSDK")]
    public bool IsSDK { get; set; }
}

