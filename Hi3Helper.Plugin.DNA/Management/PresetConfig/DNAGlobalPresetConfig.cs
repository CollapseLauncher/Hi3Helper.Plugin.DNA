using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.Core.Management.Api;
using Hi3Helper.Plugin.Core.Management.PresetConfig;
using Hi3Helper.Plugin.Core.Utility.Windows;
using Hi3Helper.Plugin.DNA.Management.Api;
using System;
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
public partial class DNAGlobalPresetConfig : PluginPresetConfigBase
{
    private const string ApiResponseUrl = "http://pan01-cdn-aws-jp.dna-herogame.com/";
    private const string CurrentUninstKey = "";
    private const string CurrentTag = "DNA_EN";
    private const string ExecutableName = "EM.exe";
    private const string VendorName = "HeroGames";

    private static readonly List<string> _supportedLanguages = ["Simplified Chinese", "Tradicional Chinese", "Japanese", "English"];

    public override string? GameName => field ??= "Duet Night Abyss";

    [field: AllowNull, MaybeNull]
    public override string GameExecutableName => field ??= ExecutableName;

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
    public override string GameLogFileName => field ??= Path.Combine("Logs", "Client.log");

    [field: AllowNull, MaybeNull]
    public override string GameRegistryKeyName => field ??= Path.GetFileNameWithoutExtension(ExecutableName);
    
    [field: AllowNull, MaybeNull]
    public override string GameVendorName => field ??= VendorName.ToLower();

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
    public override string ZoneLogoUrl => field ??= "https://shared.steamstatic.com/store_item_assets/steam/apps/3950020/268b044e824d3e28cdb44c2daa31d4f2314a8022/logo_2x.png";

    [field: AllowNull, MaybeNull]
    public override string ZonePosterUrl => field ??= "https://shared.steamstatic.com/store_item_assets/steam/apps/3950020/ee582fcde01feabf17ed4e31a54a2b9f7e7284a3/header_2x.jpg";

    [field: AllowNull, MaybeNull]
    public override string ZoneHomePageUrl => field ??= "https://duetnightabyss.dna-panstudio.com/";

    public override GameReleaseChannel ReleaseChannel => GameReleaseChannel.ClosedBeta;

    [field: AllowNull, MaybeNull]
    public override string GameMainLanguage => field ??= "en";

    [field: AllowNull, MaybeNull]
    public override string LauncherGameDirectoryName => field ??= "DNA Game";

    public override List<string> SupportedLanguages => _supportedLanguages;

    public override ILauncherApiMedia? LauncherApiMedia
    {
        get => field ??= new DNAGlobalLauncherApiMedia(ApiResponseUrl, CurrentTag, "", "");
        set;
    }

    public override ILauncherApiNews? LauncherApiNews
    {
        get => field ??= new DNAGlobalLauncherApiNews(ApiResponseUrl, CurrentTag, "", "");
        set;
    }

    public override IGameManager? GameManager
    {
        get => field ??= new DNAGameManager(ExecutableName, ApiResponseUrl, CurrentTag, "", "", CurrentUninstKey);
        set;
    }

    public override IGameInstaller? GameInstaller
    {
        get => field ??= new DNAGameInstaller(GameManager);
        set;
    }

    protected override Task<int> InitAsync(CancellationToken token)
    {
        return Task.FromResult(0);
    }
}
