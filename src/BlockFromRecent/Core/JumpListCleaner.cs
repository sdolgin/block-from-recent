using System.Runtime.InteropServices;
using OpenMcdf;

namespace BlockFromRecent.Core;

/// <summary>
/// Cleans matching entries from Windows AutomaticDestinations jump list files.
/// These OLE compound files are what Windows 11 File Explorer uses to populate
/// the "Recent" view, separate from the .lnk files in the Recent folder.
/// </summary>
public static class JumpListCleaner
{
    private static readonly string AutoDestPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Recent),
        "AutomaticDestinations");

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const int SHCNE_UPDATEDIR = 0x00001000;
    private const uint SHCNF_PATHW = 0x0005;

    /// <summary>
    /// Notify the Windows Shell that the Recent folder contents changed,
    /// so File Explorer refreshes its cached view.
    /// </summary>
    public static void NotifyShellRecentChanged()
    {
        try
        {
            string recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            IntPtr pathPtr = Marshal.StringToHGlobalUni(recentPath);
            try
            {
                SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW, pathPtr, IntPtr.Zero);
                Log.Debug("SHChangeNotify sent for Recent folder");
            }
            finally
            {
                Marshal.FreeHGlobal(pathPtr);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"SHChangeNotify failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Counts matching entries across all AutomaticDestinations files without deleting them.
    /// Used by TestRules to show the user how many jump list entries would be removed.
    /// </summary>
    public static int CountMatches(ExclusionEngine engine)
    {
        if (!Directory.Exists(AutoDestPath))
            return 0;

        int total = 0;
        foreach (var file in Directory.GetFiles(AutoDestPath, "*.automaticDestinations-ms"))
        {
            try
            {
                using var cf = new CompoundFile(file, CFSUpdateMode.ReadOnly, CFSConfiguration.Default);
                cf.RootStorage.VisitEntries(item =>
                {
                    if (item is CFStream stream && item.Name != "DestList")
                    {
                        try
                        {
                            byte[] data = stream.GetData();
                            string? target = ShortcutResolver.ResolveTargetFromBytes(data);
                            if (target != null && engine.IsExcluded(target))
                                Interlocked.Increment(ref total);
                        }
                        catch { }
                    }
                }, recursive: false);
            }
            catch { }
        }

        return total;
    }

    /// <summary>
    /// Scans all AutomaticDestinations files and removes entries matching exclusion rules.
    /// Returns the total number of entries removed across all jump list files.
    /// </summary>
    public static int CleanAll(ExclusionEngine engine)
    {
        if (!Directory.Exists(AutoDestPath))
        {
            Log.Debug("AutomaticDestinations folder not found");
            return 0;
        }

        int totalRemoved = 0;
        var files = Directory.GetFiles(AutoDestPath, "*.automaticDestinations-ms");
        Log.Debug($"JumpListCleaner: scanning {files.Length} AutomaticDestinations files");

        foreach (var file in files)
        {
            try
            {
                int removed = CleanFile(file, engine);
                totalRemoved += removed;
            }
            catch (Exception ex)
            {
                Log.Debug($"JumpListCleaner: skipped {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (totalRemoved > 0)
        {
            Log.Info($"JumpListCleaner: removed {totalRemoved} entries from AutomaticDestinations");
            NotifyShellRecentChanged();
        }

        return totalRemoved;
    }

    private static int CleanFile(string filePath, ExclusionEngine engine)
    {
        var streamsToDelete = new List<string>();

        using (var cf = new CompoundFile(filePath, CFSUpdateMode.Update, CFSConfiguration.Default))
        {
            cf.RootStorage.VisitEntries(item =>
            {
                if (item is CFStream stream && item.Name != "DestList")
                {
                    try
                    {
                        byte[] data = stream.GetData();
                        string? target = ShortcutResolver.ResolveTargetFromBytes(data);
                        if (target != null && engine.IsExcluded(target))
                        {
                            streamsToDelete.Add(item.Name);
                            Log.Debug($"JumpListCleaner: marking for removal in {Path.GetFileName(filePath)}: {item.Name} -> {target}");
                        }
                    }
                    catch
                    {
                        // Stream isn't a valid .lnk — skip
                    }
                }
            }, recursive: false);

            if (streamsToDelete.Count > 0)
            {
                foreach (var name in streamsToDelete)
                {
                    try
                    {
                        cf.RootStorage.Delete(name);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"JumpListCleaner: failed to delete stream {name}: {ex.Message}");
                    }
                }

                cf.Commit();
            }
        }

        return streamsToDelete.Count;
    }
}
