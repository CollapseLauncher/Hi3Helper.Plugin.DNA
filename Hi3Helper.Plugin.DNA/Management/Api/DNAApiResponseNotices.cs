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
    public string UniqueId { get; set; } = null!;

    public DNAApiResponseNoticesEvent Event { get; set; } = null!;
}

public class DNAApiResponseNoticesEvent
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("_incrementId")]
    public int IncrementId { get; set; }

    public List<DNAApiResponseNoticesContent> Content { get; set; } = null!;

    public string Date { get; set; } = null!;

    public string EffectiveTime { get; set; } = null!;

    public string ExpireTime { get; set; } = null!;

    [JsonPropertyName("ggType")]
    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Num { get; set; }

    public string ServiceType { get; set; } = null!;

    [JsonPropertyName("shangcicaozuo")]
    public string ShangCiCaoZuo { get; set; } = null!;

    public string TestId { get; set; } = null!;
}

public class DNAApiResponseNoticesContent
{
    public string Language { get; set; } = null!;

    public string Title { get; set; } = null!;

    [JsonPropertyName("titleBody")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("titleUrl")]
    public string ClickUrl { get; set; } = null!;
}
