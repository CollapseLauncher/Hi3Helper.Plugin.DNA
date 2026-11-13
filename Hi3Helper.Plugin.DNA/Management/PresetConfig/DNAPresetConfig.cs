using Hi3Helper.Plugin.Core.Management.PresetConfig;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public abstract partial class DNAPresetConfig : PluginPresetConfigBase
{
    public abstract string? StartExecutableName { get; }

    protected const string BackgroundUrl = "https://cdnstatic.yingxiong.com/dna_hd_1_7/imgs/home/pc/bg.webp";

    protected const string SteamDataUrl = "https://store.steampowered.com/api/appdetails?appids=3950020&l=";
}
