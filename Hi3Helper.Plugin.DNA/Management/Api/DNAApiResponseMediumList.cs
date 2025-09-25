using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseMediumList
{
    [JsonPropertyName("UniqueId")]
    public string? UniqueId { get; set; }

    [JsonPropertyName("mediumList")]
    public List<DNAApiResponseMedium>? MediumList { get; set; }
}
