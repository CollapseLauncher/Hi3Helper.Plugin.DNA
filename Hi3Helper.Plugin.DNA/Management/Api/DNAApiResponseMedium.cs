// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseMedium
{
    [JsonPropertyName("content")]
    public List<MediumContent>? Content { get; set; }

    [JsonPropertyName("medium")]
    public MediumIdentifier? Medium {  get; set; }

    public class MediumIdentifier
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

    public class MediumInlet
    {
        [JsonPropertyName("inletName")]
        public string? Name { get; set; }

        [JsonPropertyName("inletType")]
        public string? Type { get; set; }

        [JsonPropertyName("inletUrl")]
        public string? Url { get; set; }
    }

    public class MediumContent
    {
        [JsonPropertyName("body")]
        public List<MediumInlet>? Inlets { get; set; }

        [JsonPropertyName("language")]
        public MediumIdentifier? Language { get; set; }
    }
}
