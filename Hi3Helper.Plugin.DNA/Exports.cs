using Hi3Helper.Plugin.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

namespace Hi3Helper.Plugin.DNA;

/// <summary>
/// Provides necessary unmanaged API exports for the plugin to be loaded.<br/><br/>
/// 
/// </summary>
public partial class DNAbyss : SharedStaticV1Ext<DNAbyss> // 2025-08-18: We use generic version of SharedStatic<T> to add support for game launch API.
                                                       //             Though, the devs can still use the old SharedStatic without any compatibility issue.
{
    static DNAbyss() => Load<DNAPlugin>(!RuntimeFeature.IsDynamicCodeCompiled ? new Core.Management.GameVersion(0, 2, 1, 0) : default); // Loads the IPlugin instance as DNAPlugin.

    [UnmanagedCallersOnly(EntryPoint = "TryGetApiExport", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int TryGetApiExport(char* exportName, void** delegateP) =>
        TryGetApiExportPointer(exportName, delegateP);
}