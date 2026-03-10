using BlockFromRecent.Config;

namespace BlockFromRecent.Core;

public class RecentFileCleaner : IDisposable
{
    private readonly RecentFileWatcher _watcher;
    private readonly ExclusionEngine _engine;
    private System.Timers.Timer? _periodicScanTimer;
    private AppConfig _config;

    public event Action<string, string>? OnFileRemoved; // (lnkPath, targetPath)

    public RecentFileCleaner(AppConfig config)
    {
        _config = config;
        _engine = new ExclusionEngine();
        _engine.UpdateRules(config.Rules);
        _watcher = new RecentFileWatcher();
        _watcher.OnNewRecentFile += HandleNewRecentFile;
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        _engine.UpdateRules(config.Rules);
        ConfigurePeriodicScan(config.PeriodicScanIntervalMinutes);
    }

    public void Start()
    {
        if (_config.ScanOnStartup)
            ScanExisting();

        _watcher.Start();
        ConfigurePeriodicScan(_config.PeriodicScanIntervalMinutes);
    }

    public void Stop()
    {
        _watcher.Stop();
        _periodicScanTimer?.Stop();
    }

    /// <summary>
    /// Scans all existing .lnk files in the Recent folder and removes matches.
    /// Also cleans matching entries from AutomaticDestinations jump lists.
    /// Returns the number of files removed.
    /// </summary>
    public int ScanExisting()
    {
        int removed = 0;
        string recentPath = RecentFileWatcher.RecentFolderPath;

        try
        {
            var files = Directory.GetFiles(recentPath, "*.lnk");
            Log.Info($"ScanExisting: found {files.Length} .lnk files in {recentPath}");

            foreach (var lnkFile in files)
            {
                if (TryRemoveIfExcluded(lnkFile))
                    removed++;
            }

            // Also clean AutomaticDestinations jump list entries
            int jumpListRemoved = JumpListCleaner.CleanAll(_engine);
            removed += jumpListRemoved;

            if (removed > 0)
                JumpListCleaner.NotifyShellRecentChanged();

            Log.Info($"ScanExisting complete: removed {removed} item(s) (jump list: {jumpListRemoved})");
        }
        catch (Exception ex)
        {
            Log.Error("ScanExisting failed", ex);
        }

        return removed;
    }

    private void ConfigurePeriodicScan(int intervalMinutes)
    {
        _periodicScanTimer?.Stop();
        _periodicScanTimer?.Dispose();
        _periodicScanTimer = null;

        if (intervalMinutes <= 0)
        {
            Log.Info("Periodic scan disabled");
            return;
        }

        _periodicScanTimer = new System.Timers.Timer(intervalMinutes * 60_000) { AutoReset = true };
        _periodicScanTimer.Elapsed += (_, _) =>
        {
            Log.Info("Periodic scan triggered");
            ScanExisting();
        };
        _periodicScanTimer.Start();
        Log.Info($"Periodic scan configured: every {intervalMinutes} minute(s)");
    }

    private void HandleNewRecentFile(string lnkPath)
    {
        RetryWithDelay(() =>
        {
            bool removed = TryRemoveIfExcluded(lnkPath);
            if (removed)
            {
                // Also clean the matching entry from jump list databases
                // so it disappears from Explorer's Recent view
                try { JumpListCleaner.CleanAll(_engine); } catch { }
                JumpListCleaner.NotifyShellRecentChanged();
            }
            return removed;
        }, maxRetries: 3, delayMs: 200);
    }

    private bool TryRemoveIfExcluded(string lnkPath)
    {
        try
        {
            if (!File.Exists(lnkPath))
            {
                Log.Debug($"TryRemove: {Path.GetFileName(lnkPath)} no longer exists, skipping");
                return false;
            }

            string? target = ShortcutResolver.ResolveTarget(lnkPath);
            if (target == null)
            {
                Log.Debug($"TryRemove: {Path.GetFileName(lnkPath)} target could not be resolved");
                return false;
            }

            if (_engine.IsExcluded(target))
            {
                File.Delete(lnkPath);
                Log.Info($"Removed: {Path.GetFileName(lnkPath)} -> {target}");
                OnFileRemoved?.Invoke(lnkPath, target);
                return true;
            }
            else
            {
                Log.Debug($"TryRemove: {Path.GetFileName(lnkPath)} not excluded (target: {target})");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"TryRemoveIfExcluded failed for {Path.GetFileName(lnkPath)}", ex);
        }

        return false;
    }

    private static void RetryWithDelay(Func<bool> action, int maxRetries, int delayMs)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            if (action())
                return;

            if (i < maxRetries - 1)
                Thread.Sleep(delayMs);
        }
    }

    public void Dispose()
    {
        _periodicScanTimer?.Dispose();
        _watcher.Dispose();
    }
}
