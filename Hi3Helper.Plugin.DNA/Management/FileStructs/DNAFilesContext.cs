using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.FileStructs;

[JsonSerializable(typeof(DNAFilesVersion))]
public partial class DNAFilesContext : JsonSerializerContext;
