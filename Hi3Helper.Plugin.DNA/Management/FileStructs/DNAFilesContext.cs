using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.FileStructs;

[JsonSerializable(typeof(DNAFilesGameVersion))]
[JsonSerializable(typeof(DNAFilesDownloadVersion))]
public partial class DNAFilesContext : JsonSerializerContext;
