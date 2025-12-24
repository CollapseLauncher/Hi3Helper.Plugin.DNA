using Hi3Helper.Plugin.Core.Management;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public partial class DNAEpicPresetConfig : DNAGlobalPresetConfig
{
    private const string ApiResponseUrl = "http://pan01-cdn-hs-jp.dna-panstudio.com/";
    private const string ApiResponseTag = "PC_OBT_Global_Epic_Pub";

    [field: AllowNull, MaybeNull]
    public override string ProfileName => field ??= "DNAEpic";

    [field: AllowNull, MaybeNull]
    public override string ZoneName => field ??= "Epic Games";

    [field: AllowNull, MaybeNull]
    public override string ZoneFullName => field ??= "Duet Night Abyss (Epic Games)";

    public override IGameManager? GameManager
    {
        get => field ??= new DNAGameManager(ExecutableName, ApiResponseUrl, ApiResponseTag, this);
        set;
    }

    public override IGameInstaller? GameInstaller
    {
        get => field ??= new DNAGameInstaller(GameManager, ApiResponseUrl, ApiResponseTag);
        set;
    }
}
