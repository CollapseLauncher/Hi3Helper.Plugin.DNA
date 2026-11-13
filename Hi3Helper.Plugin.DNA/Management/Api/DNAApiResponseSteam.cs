// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseSteam : Dictionary<string, DNAApiResponseSteamApp>;

public class DNAApiResponseSteamApp
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public DNAApiResponseSteamData? Data { get; set; }
}

public class DNAApiResponseSteamData
{
    [JsonPropertyName("movies")]
    public List<DNAApiResponseSteamMovie>? Movies { get; set; }

    [JsonPropertyName("screenshots")]
    public List<DNAApiResponseSteamScreenshot>? Screenshots { get; set; }

    [JsonPropertyName("background")]
    public string? Background { get; set; }

    [JsonPropertyName("background_raw")]
    public string? BackgroundRaw { get; set; }

    [JsonPropertyName("header_image")]
    public string? HeaderImage { get; set; }

    [JsonPropertyName("capsule_image")]
    public string? CapsuleImage { get; set; }

    [JsonPropertyName("capsule_imagev5")]
    public string? CapsuleImageV5 { get; set; }
}

public class DNAApiResponseSteamMovie
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("dash_av1")]
    public string? DashAv1 { get; set; }

    [JsonPropertyName("dash_h264")]
    public string? DashH264 { get; set; }

    [JsonPropertyName("hls_h264")]
    public string? HlsH264 { get; set; }

    [JsonPropertyName("highlight")]
    public bool Highlight { get; set; }
}

public class DNAApiResponseSteamScreenshot
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("path_thumbnail")]
    public string? PathThumbnail { get; set; }

    [JsonPropertyName("path_full")]
    public string? PathFull { get; set; }
}
