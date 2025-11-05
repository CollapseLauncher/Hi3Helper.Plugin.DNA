using Hi3Helper.Plugin.Core.Management.PresetConfig;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public abstract partial class DNAPresetConfig : PluginPresetConfigBase
{
    public abstract string? StartExecutableName { get; }
}
