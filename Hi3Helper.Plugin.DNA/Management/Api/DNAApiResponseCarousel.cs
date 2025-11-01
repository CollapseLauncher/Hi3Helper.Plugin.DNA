// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseCarousel : Dictionary<string, DNAApiResponseCarouselEntry> { }

public class DNAApiResponseCarouselEntry
{
    [JsonPropertyName("EndTimestamp")]
    public long EndTimestamp { get; set; }

    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("_incrementId")]
    public int IncrementId { get; set; }

    [JsonPropertyName("area")]
    public DNAApiResponseCarouselArea Area { get; set; } = new();

    [JsonPropertyName("content")]
    public List<DNAApiResponseCarouselContent> Content { get; set; } = [];

    [JsonPropertyName("effectiveTime")]
    public string? EffectiveTime { get; set; }

    [JsonPropertyName("expireTime")]
    public string? ExpireTime { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;

    [JsonPropertyName("serviceType")]
    public string? ServiceType { get; set; }

    [JsonPropertyName("shangcicaozuo")]
    public int ShangCiCaoZuo { get; set; }
}

public class DNAApiResponseCarouselArea
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class DNAApiResponseCarouselContent
{
    [JsonPropertyName("headImages")]
    public List<DNAApiResponseCarouselHeadImage> HeadImages { get; set; } = [];

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}

public class DNAApiResponseCarouselHeadImage
{
    [JsonPropertyName("image")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("imageName")]
    public string? Description { get; set; }

    [JsonPropertyName("imageURL")]
    public string? ClickUrl { get; set; }

    [JsonPropertyName("num")]
    public int Num { get; set; }
}

