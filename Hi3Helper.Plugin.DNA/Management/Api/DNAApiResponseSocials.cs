// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseSocials
{
    [JsonPropertyName("UniqueId")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("mediumList")]
    public List<DNAApiResponseMedium>? MediumList { get; set; }
}

public class DNAApiResponseMedium
{
    [JsonPropertyName("content")]
    public List<DNAApiResponseMediumContent>? Content { get; set; }

    [JsonPropertyName("medium")]
    public DNAApiResponseMediumIdentifier? Medium {  get; set; } 
}

public class DNAApiResponseMediumIdentifier
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class DNAApiResponseMediumInlet
{
    [JsonPropertyName("inletName")]
    public string? Name { get; set; }

    [JsonPropertyName("inletType")]
    public string? Type { get; set; }

    [JsonPropertyName("inletUrl")]
    public string? Url { get; set; }
}

public class DNAApiResponseMediumContent
{
    [JsonPropertyName("body")]
    public List<DNAApiResponseMediumInlet>? Inlets { get; set; }

    [JsonPropertyName("language")]
    public DNAApiResponseMediumIdentifier? Language { get; set; }
}

