// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseVersion
{
    [JsonPropertyName("GameVersionList")]
    public Dictionary<string, DNAApiResponseVersionContainer> GameVersionList { get; set; } = [];
}

public class DNAApiResponseVersionContainer
{
    [JsonPropertyName("GameVersionList")]
    public Dictionary<string, DNAApiResponseVersionFileInfo> GameVersionList { get; set; } = [];
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
}
