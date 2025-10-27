// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseBackground : Dictionary<string, DNAApiResponseBackgroundEntry> { }

public class DNAApiResponseBackgroundEntry
{
    [JsonPropertyName("EndTimestamp")]
    public long EndTimestamp { get; set; }

    [JsonPropertyName("_id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("_incrementId")]
    public int IncrementId { get; set; }

    public DNAApiResponseBackgroundArea Area { get; set; } = null!;

    public List<DNAApiResponseBackgroundContent> Content { get; set; } = null!;

    public string EffectiveTime { get; set; } = null!;

    public string ExpireTime { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ServiceType { get; set; } = null!;

    [JsonPropertyName("shangcicaozuo")]
    public int ShangCiCaoZuo { get; set; }
}

public class DNAApiResponseBackgroundArea
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}

public class DNAApiResponseBackgroundContent
{
    public List<DNAApiResponseBackgroundHeadImage> HeadImages { get; set; } = null!;

    public string Language { get; set; } = null!;
}

public class DNAApiResponseBackgroundHeadImage
{
    [JsonPropertyName("image")]
    public string Image { get; set; } = null!;

    [JsonPropertyName("imageName")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("imageURL")]
    public string URL { get; set; } = null!;

    [JsonPropertyName("num")]
    public int Num { get; set; }
}

