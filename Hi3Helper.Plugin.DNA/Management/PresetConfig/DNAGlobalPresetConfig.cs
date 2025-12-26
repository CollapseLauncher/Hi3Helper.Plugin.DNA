using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Management.Api;
using Hi3Helper.Plugin.DNA.Management.Api;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.PresetConfig;

[GeneratedComClass]
public partial class DNAGlobalPresetConfig : DNAPresetConfig
{
    private readonly DNAApiResponseDetails ApiResponseDetails = new()
    {
        BaseUrls = [
            "http://pan01-cdn-aws-jp.dna-panstudio.com",
            "http://pan01-cdn-ali-jp.dna-panstudio.com",
            "http://pan01-cdn-eo-jp.dna-panstudio.com",
            "http://pan01-cdn-hs-jp.dna-panstudio.com",
        ],
        Tag = "PC_OBT_Global_Pub",
        Region = "Global",
        RegionLong = "Global"
    };

    protected const string ExecutableName = "EM.exe";
    private const string EngineExecutableName = "EM-Win64-Shipping.exe";
    private const string VendorName = "Hero Games";
    protected const string BackgroundUrl = "https://video.yingxiong.com/fhd/2899ab9bef7c4a9eaf971b43fc109ec4.mp4";
    
    private static readonly List<string> _supportedLanguages = ["Simplified Chinese", "Tradicional Chinese", "Japanese", "English"];

    public override string GameName => "Duet Night Abyss";

    [field: AllowNull, MaybeNull]
    public override string GameExecutableName => field ??= Path.Combine("EM", "Binaries", "Win64", EngineExecutableName);
    
    [field: AllowNull, MaybeNull]
    public override string StartExecutableName => field ??= ExecutableName;

    public override string GameAppDataPath { 
        get
        {
            string? path = null;
            GameManager?.GetGamePath(out path);
            if (path == null)
                return string.Empty;
            return Path.Combine(path, "EM", "Saved");
        }
    }

    [field: AllowNull, MaybeNull]
    public override string GameLogFileName => field ??= Path.Combine("Logs", "EM.log");

    [field: AllowNull, MaybeNull]
    public override string GameRegistryKeyName => field ??= GameName;
    
    [field: AllowNull, MaybeNull]
    public override string GameVendorName => field ??= VendorName;

    [field: AllowNull, MaybeNull]
    public override string ProfileName => field ??= "DNAGlobal";

    [field: AllowNull, MaybeNull]
    public override string ZoneDescription => field ??=
        "Duet Night Abyss is a fantasy adventure RPG with a high degree of freedom developed by Hero Games' Pan Studio. " +
        "The game features \"multiple weapon loadouts & 3D combat\" at its core, and tells the story of \"Demons\" from dual perspectives.";

    [field: AllowNull, MaybeNull]
    public override string ZoneName => field ??= "Global";

    [field: AllowNull, MaybeNull]
    public override string ZoneFullName => field ??= "Duet Night Abyss (Global)";

    [field: AllowNull, MaybeNull]
    public override string ZoneLogoUrl => field ??= "https://cdnstatic.herogame.com/static/duetnightabyss/4.0/imgs/icon/black-logo-en.png";

    [field: AllowNull, MaybeNull]
    public override string ZonePosterUrl => field ??= "https://cdnstatic.herogame.com/static/duetnightabyss/4.0/imgs/worldview/pc/2.jpg";

    [field: AllowNull, MaybeNull]
    public override string ZoneHomePageUrl => field ??= "https://duetnightabyss.dna-panstudio.com/";

    public override GameReleaseChannel ReleaseChannel => GameReleaseChannel.Public;

    [field: AllowNull, MaybeNull]
    public override string GameMainLanguage => field ??= "en";

    [field: AllowNull, MaybeNull]
    public override string LauncherGameDirectoryName => field ??= "DNA Game";

    public override List<string> SupportedLanguages => _supportedLanguages;

    public override ILauncherApiMedia? LauncherApiMedia
    {
        get => field ??= new DNAGlobalLauncherApiMedia(ApiResponseDetails, ZoneLogoUrl, BackgroundUrl);
        set;
    }

    public override ILauncherApiNews? LauncherApiNews
    {
        get => field ??= new DNAGlobalLauncherApiNews(ApiResponseDetails);
        set;
    }

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

    protected override Task<int> InitAsync(CancellationToken token)
    {
        return Task.FromResult(0);
    }
}
