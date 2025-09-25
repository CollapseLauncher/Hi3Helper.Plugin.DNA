using Hi3Helper.Plugin.Core.DiscordPresence;

namespace Hi3Helper.Plugin.DNA;

public partial class DNAbyss
{
    private const ulong DiscordPresenceId = 1420753315582709800u;
    private const string DiscordPresenceLargeIconUrl = "https://play-lh.googleusercontent.com/JynqYikH08XZ7XgRUzobGY2fIP8T3JwdqseP_y93B7acIy5VSFAHWaRVz-ACFTQACPjJJdmh9fVkJacYwWeNbQ";

    protected override bool GetCurrentDiscordPresenceInfoCore(
        DiscordPresenceExtension.DiscordPresenceContext context,
        out ulong presenceId,
        out string? largeIconUrl,
        out string? largeIconTooltip,
        out string? smallIconUrl,
        out string? smallIconTooltip)
    {
        presenceId = DiscordPresenceId;
        largeIconUrl = DiscordPresenceLargeIconUrl;
        largeIconTooltip = null;
        smallIconUrl = null;
        smallIconTooltip = null;

        return true;
    }
}