// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponsePatches
{
    [JsonPropertyName("versionList")]
    public Dictionary<string, DNAApiResponsePatchesVersion> VersionList { get; set; } = [];
}

public class DNAApiResponsePatchesVersion
{
    [JsonPropertyName("major")]
    public int Major { get; set; }

    [JsonPropertyName("minor")]
    public int Minor { get; set; }

    [JsonPropertyName("revamp")]
    public int Revamp { get; set; }

    [JsonPropertyName("patchKey")]
    public int PatchKey { get; set; }

    [JsonPropertyName("patchVersion")]
    public int PatchVersion { get; set; }

    [JsonPropertyName("bOptional")]
    public bool BOptional { get; set; }

    [JsonPropertyName("bRestart")]
    public bool BRestart { get; set; }
}

