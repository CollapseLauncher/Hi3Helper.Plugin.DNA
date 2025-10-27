using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api.Response;

[JsonSerializable(typeof(DNAApiResponseSocials))]
[JsonSerializable(typeof(DNAApiResponseNotices))]
[JsonSerializable(typeof(DNAApiResponseBackground))]
public partial class DNAApiResponseContext : JsonSerializerContext;
