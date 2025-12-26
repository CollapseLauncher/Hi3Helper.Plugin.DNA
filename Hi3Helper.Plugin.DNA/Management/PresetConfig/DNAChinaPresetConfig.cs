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
public partial class DNAChinaPresetConfig : DNAPresetConfig
{
    private readonly DNAApiResponseDetails ApiResponseDetails = new()
    {
        BaseUrls = [
            "http://pan01-cdn-dna-ali.shyxhy.com",
            "http://pan01-cdn-dna-aws.shyxhy.com",
            "http://pan01-1-eo.shyxhy.com",
            "http://pan01-1-hs.shyxhy.com"
        ],
        Tag = "PC_OBT_CN_Pub",
        Region = "CN",
        RegionLong = "China"
    };

    protected const string ExecutableName = "EM.exe";
    private const string EngineExecutableName = "EM-Win64-Shipping.exe";
    private const string VendorName = "Hero Games";
    protected const string BackgroundUrl = "https://video.yingxiong.com/fhd/2899ab9bef7c4a9eaf971b43fc109ec4.mp4";
    
    private static readonly List<string> _supportedLanguages = ["Simplified Chinese", "Tradicional Chinese"];

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
    public override string GameRegistryKeyName => field ??= "二重螺旋";
    
    [field: AllowNull, MaybeNull]
    public override string GameVendorName => field ??= VendorName;

    [field: AllowNull, MaybeNull]
    public override string ProfileName => field ??= "DNAChina";

    [field: AllowNull, MaybeNull]
    public override string ZoneDescription => field ??=
        "《二重螺旋》是英雄游戏旗下潘神工作室自研的一款幻想风多维战斗爽游。\n游戏以「多维武器组合×立体战斗」为核心玩法，以「双视角」讲述「恶魔」的故事。";

    [field: AllowNull, MaybeNull]
    public override string ZoneName => field ??= "Mainland China";

    [field: AllowNull, MaybeNull]
    public override string ZoneFullName => field ??= "Duet Night Abyss (Mainland China)";

    [field: AllowNull, MaybeNull]
    public override string ZoneLogoUrl => field ??= "https://cdnstatic.yingxiong.com/dna/gw/imgs/icon/black-logo.png";

    [field: AllowNull, MaybeNull]
    public override string ZonePosterUrl => field ??= "https://cdnstatic.herogame.com/static/duetnightabyss/4.0/imgs/worldview/pc/2.jpg";

    [field: AllowNull, MaybeNull]
    public override string ZoneHomePageUrl => field ??= "https://dna.yingxiong.com/";

    public override GameReleaseChannel ReleaseChannel => GameReleaseChannel.Public;

    [field: AllowNull, MaybeNull]
    public override string GameMainLanguage => field ??= "cn";

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
        get => field ??= new DNAGlobalLauncherApiNews(ApiResponseDetails, "CN");
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
