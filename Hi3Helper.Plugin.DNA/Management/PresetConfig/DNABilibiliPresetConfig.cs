using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.DNA.Management.Api;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public partial class DNABiliBilliPresetConfig : DNAChinaPresetConfig
{
    private readonly DNAApiResponseDetails ApiResponseDetails = new()
    {
        BaseUrls = [
            "http://pan01-cdn-dna-ali.shyxhy.com",
            "http://pan01-cdn-dna-aws.shyxhy.com",
            "http://pan01-1-eo.shyxhy.com",
            "http://pan01-1-hs.shyxhy.com"
        ],
        Tag = "PC_OBT_Bili_Pub",
        Region = "CN",
        RegionLong = "China"
    };

    [field: AllowNull, MaybeNull]
    public override string GameRegistryKeyName => field ??= base.GameRegistryKeyName + " bilibili";

    [field: AllowNull, MaybeNull]
    public override string ProfileName => field ??= "DNABilibili";

    [field: AllowNull, MaybeNull]
    public override string ZoneName => field ??= "Bilibili";

    [field: AllowNull, MaybeNull]
    public override string ZoneFullName => field ??= "Duet Night Abyss (Bilibili)";

    public override IGameManager? GameManager
    {
        get => field ??= new DNAGameManager(ExecutableName, ApiResponseDetails, this);
        set;
    }

    public override IGameInstaller? GameInstaller
    {
        get => field ??= new DNAGameInstaller(GameManager, ApiResponseDetails);
        set;
    }
}
