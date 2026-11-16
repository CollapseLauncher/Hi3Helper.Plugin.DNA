using Hi3Helper.Plugin.Core.Management.PresetConfig;
using Hi3Helper.Plugin.DNA.Management.Api;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public abstract partial class DNAPresetConfig : PluginPresetConfigBase
{
    public abstract DNAApiDetails ApiResponseDetails { get; }

    public abstract string? StartExecutableName { get; }

    public string DefaultBackgroundUrl { get; } = "https://cdnstatic.yingxiong.com/dna_hd_1_3/imgs/home/pc/bg.webp";
}
