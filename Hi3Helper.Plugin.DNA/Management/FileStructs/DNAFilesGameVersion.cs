// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.FileStructs;

// <root>/GameVersion.json
public class DNAFilesGameVersion
{
    [JsonPropertyName("version")]
    public int? Version { get; set; } = null!;
}
