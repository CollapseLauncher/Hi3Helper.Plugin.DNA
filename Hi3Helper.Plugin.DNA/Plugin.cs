using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management.PresetConfig;
using Hi3Helper.Plugin.Core.Update;
using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.PresetConfig;
using Hi3Helper.Plugin.DNA.Utility;
using System;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable InconsistentNaming
namespace Hi3Helper.Plugin.DNA;

[GeneratedComClass]
public partial class DNAPlugin : PluginBase
{
    private static readonly IPluginPresetConfig[] PresetConfigInstances = [ new DNAGlobalPresetConfig() ];
    private static DateTime _pluginCreationDate = new(2025, 09, 06, 23, 08, 0, DateTimeKind.Utc);
    private static IPluginSelfUpdate? _selfUpdaterInstance;

    public override void GetPluginName(out string result) => result = "Duet Night Abyss Plugin";

    public override void GetPluginDescription(out string result) => result = "A plugin for Duet Night Abyss on Collapse Launcher";

    public override void GetPluginAuthor(out string result) => result = "gablm, Collapse Project Team";

    public override unsafe void GetPluginCreationDate(out DateTime* result) => result = _pluginCreationDate.AsPointer();

    public override void GetPresetConfigCount(out int count) => count = PresetConfigInstances.Length;

    public override void GetPresetConfig(int index, out IPluginPresetConfig presetConfig)
    {
        // Avoid crash by returning null if index is out of bounds
        if (index < 0 || index >= PresetConfigInstances.Length)
        {
            presetConfig = null!;
            return;
        }

        // Return preset config at index (n)
        presetConfig = PresetConfigInstances[index];
    }

    public override void GetPluginSelfUpdater(out IPluginSelfUpdate selfUpdate) => selfUpdate = _selfUpdaterInstance ??= new DNAPluginSelfUpdate();

    private string? _getPluginAppIconUrl;
    public override void GetPluginAppIconUrl(out string result) => result = _getPluginAppIconUrl ??= Convert.ToBase64String(DNAIconData.DNAAppIconData);

    private string? _getNotificationPosterUrl;
    public override void GetNotificationPosterUrl(out string result) => result = _getNotificationPosterUrl ??= Convert.ToBase64String(DNAIconData.DNAAppPosterData);
}
