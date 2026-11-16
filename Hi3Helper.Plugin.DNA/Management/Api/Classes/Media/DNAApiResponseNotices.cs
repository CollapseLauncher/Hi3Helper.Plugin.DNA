// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseNotices : Dictionary<string, DNAApiResponseNoticesEntry> { }

public class DNAApiResponseNoticesEntry
{
    [JsonPropertyName("EndTimestamp")]
    public long EndTimestamp { get; set; }

    [JsonPropertyName("UniqueId")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("event")]
    public DNAApiResponseNoticesEvent Event { get; set; } = new();
}

public class DNAApiResponseNoticesEvent
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("_incrementId")]
    public int IncrementId { get; set; }

    [JsonPropertyName("content")]
    public List<DNAApiResponseNoticesContent> Content { get; set; } = [];

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("effectiveTime")]
    public string? EffectiveTime { get; set; }

    [JsonPropertyName("expireTime")]
    public string? ExpireTime { get; set; }

    [JsonPropertyName("ggType")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("serviceType")]
    public string? ServiceType { get; set; }

    [JsonPropertyName("shangcicaozuo")]
    public string? ShangCiCaoZuo { get; set; }

    [JsonPropertyName("testId")]
    public string? TestId { get; set; }
}

public class DNAApiResponseNoticesContent
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("titleBody")]
    public string? Description { get; set; }

    [JsonPropertyName("titleUrl")]
    public string? ClickUrl { get; set; }
}
