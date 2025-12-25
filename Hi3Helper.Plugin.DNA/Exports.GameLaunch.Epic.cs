using Hi3Helper.Plugin.Core.Utility;
using Hi3Helper.Plugin.DNA.Management.PresetConfig;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hi3Helper.Plugin.DNA;

public partial class DNAbyss
{
    private const string EpicLaunchUri = "com.epicgames.launcher://apps/ceb703de37854cda9932d2f9dd531520%3A572fcef540b841dd9230d813bd5208fe%3A58c7aa4f48be44f3b40f5f78f62c84b7?action=launch&silent=true";
    private bool IsEpicLoading = false;
    private DateTime? EpicStartTime = null;
    private Process[] EpicProcesses = [];

    private const int SW_MINIMIZE = 6;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private async Task<bool> TryInitializeEpicLauncher(GameManagerExtension.RunGameFromGameManagerContext context, CancellationToken token)
    {
        if (context.PresetConfig is not DNAEpicPresetConfig presetConfig)
        {
            return true;
        }

        IsEpicLoading = true;
        EpicStartTime = DateTime.Now;

        // Trigger launcher via Epic
        ProcessStartInfo psi = new()
        {
            FileName = EpicLaunchUri,
            UseShellExecute = true
        };
        Process.Start(psi);
        
        // Find main process for launcher
        int delay = 0;
        while (EpicProcesses.Length == 0 && delay < 15000)
        {
            EpicProcesses = Process.GetProcessesByName("EMLauncher-Win64-Shipping");

            await Task.Delay(200, token);
            delay += 200;
        }

        if (EpicProcesses.Length > 0)
        {
            Process p = EpicProcesses.First();
            while (p.MainWindowHandle == IntPtr.Zero)
            {
                p.Refresh();
                await Task.Delay(100, token);
            }

            // Minimize launcher window
            ShowWindow(p.MainWindowHandle, SW_MINIMIZE);
        }
        
        IsEpicLoading = false;

        return true;
    }

    private async Task TryKillEpicLauncher(GameManagerExtension.RunGameFromGameManagerContext context, CancellationToken token)
    { 
        try
        {
            // Give some time for the launcher to init EOS for the game
            await Task.Delay(15000, CancellationToken.None);

            // Kill launcher
            foreach (var p in EpicProcesses)
            {
                p.Kill();
                p.Dispose();
            }
        }
        catch (Exception)
        {
            // Pass
        }
        finally
        {
            EpicProcesses = [];
        }
    }
}
