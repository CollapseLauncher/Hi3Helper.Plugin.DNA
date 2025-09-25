using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

[JsonSerializable(typeof(DNAApiResponseMediumList))]
[JsonSerializable(typeof(DNAApiResponseMedium))]
[JsonSerializable(typeof(DNAApiResponseMedium.MediumIdentifier))]
[JsonSerializable(typeof(DNAApiResponseMedium.MediumInlet))]
[JsonSerializable(typeof(DNAApiResponseMedium.MediumContent))]
public partial class DNAApiResponseContext : JsonSerializerContext;
