// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.FileStructs;

public class DNAFilesVersion
{
    [JsonPropertyName("gameVersionList")]
    public Dictionary<string, DNAFilesVersionContainer> GameVersionList { get; set; } = [];
}

public class DNAFilesVersionContainer
{
    [JsonPropertyName("gameVersionList")]
    public Dictionary<string, DNAFilesVersionFileInfo> GameVersionList { get; set; } = [];
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

