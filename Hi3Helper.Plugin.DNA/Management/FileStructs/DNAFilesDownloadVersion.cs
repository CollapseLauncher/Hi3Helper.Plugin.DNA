// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.FileStructs;

// <root>/TempPath/TempGameFiles/GameVersion.json
public class DNAFilesDownloadVersion
{
    [JsonPropertyName("version")]
    public string? Version { get; set; } = null!;

    [JsonPropertyName("hdiff_file")]
    public string? HdiffFile { get; set; } = null!;

    [JsonPropertyName("md5")]
    public string? MD5 { get; set; } = null!;

    [JsonPropertyName("is_full_patch")]
    public bool? IsFullPatch { get; set; } = null!;
}
