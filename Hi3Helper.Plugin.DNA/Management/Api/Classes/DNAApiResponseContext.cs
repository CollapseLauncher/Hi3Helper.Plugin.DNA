using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

[JsonSerializable(typeof(DNAApiResponseSocials))]
[JsonSerializable(typeof(DNAApiResponseNotices))]
[JsonSerializable(typeof(DNAApiResponseCarousel))]
[JsonSerializable(typeof(DNAApiResponsePatches))]
[JsonSerializable(typeof(DNAApiResponseHdiffInfo))]
[JsonSerializable(typeof(DNAApiResponseVersionManifest))]
public partial class DNAApiResponseContext : JsonSerializerContext;
