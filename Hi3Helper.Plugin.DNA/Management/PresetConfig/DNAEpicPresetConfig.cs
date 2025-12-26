using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.DNA.Management.Api;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public partial class DNAEpicPresetConfig : DNAGlobalPresetConfig
{
    private readonly DNAApiResponseDetails ApiResponseDetails = new()
    {
        BaseUrls = [
            "http://pan01-cdn-aws-jp.dna-panstudio.com",
            "http://pan01-cdn-ali-jp.dna-panstudio.com",
            "http://pan01-cdn-eo-jp.dna-panstudio.com",
            "http://pan01-cdn-hs-jp.dna-panstudio.com",
        ],
        Tag = "PC_OBT_Global_Epic_Pub",
        Region = "Global",
        RegionLong = "Global"
    };

    [field: AllowNull, MaybeNull]
    public override string ProfileName => field ??= "DNAEpic";

    [field: AllowNull, MaybeNull]
    public override string ZoneName => field ??= "Epic Games";

    [field: AllowNull, MaybeNull]
    public override string ZoneFullName => field ??= "Duet Night Abyss (Epic Games)";

    [field: AllowNull, MaybeNull]
    public override string ZoneHomePageUrl => field ??= "https://store.epicgames.com/en-US/p/duetnightabyss-016366";

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
