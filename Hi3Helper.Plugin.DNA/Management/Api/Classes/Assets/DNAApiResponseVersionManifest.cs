// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseVersionManifest
{
    [JsonPropertyName("latest_version")]
    public string? LatestVersion { get; set; } = null!;

    [JsonPropertyName("latest_version_number")]
    public string? LatestVersionNumber { get; set; } = null!;

    [JsonPropertyName("min_supported_version")]
    public string? MinSupportedVersion { get; set; } = null!;
}

