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
    private const string ApiResponseUrl = "http://pan01-cdn-aws-jp.dna-panstudio.com/";
    private const string ExecutableName = "EM.exe";
    private const string EngineExecutableName = "EM-Win64-Shipping.exe";
    private const string VendorName = "Hero Games";
    private const string BackgroundUrl = "https://video.yingxiong.com/fhd/50a2815d8e0948109da1deb9c24c5360.mp4";

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
        get => field ??= new DNAGlobalLauncherApiMedia(ApiResponseUrl, ZoneLogoUrl, BackgroundUrl);
        set;
    }

    public override ILauncherApiNews? LauncherApiNews
    {
        get => field ??= new DNAGlobalLauncherApiNews(ApiResponseUrl);
        set;
    }

    public override IGameManager? GameManager
    {
        get => field ??= new DNAGameManager(ExecutableName, ApiResponseUrl, this);
        set;
    }

    public override IGameInstaller? GameInstaller
    {
        get => field ??= new DNAGameInstaller(GameManager, ApiResponseUrl);
        set;
    }

    protected override Task<int> InitAsync(CancellationToken token)
    {
        return Task.FromResult(0);
    }
}
