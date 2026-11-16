// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseHdiffInfo
{
    [JsonPropertyName("hdiff_file")]
    public DNAApiResponseHdiffFile? HdiffFile { get; set; } = null!;

    [JsonPropertyName("new_size")]
    public long? NewSize { get; set; } = null!;
}

public class DNAApiResponseHdiffFile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    [JsonPropertyName("md5")]
    public string? MD5 { get; set; } = null!;

    [JsonPropertyName("size")]
    public long? Size { get; set; } = null!;
}