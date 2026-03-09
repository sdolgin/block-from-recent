using BlockFromRecent.Config;

namespace BlockFromRecent.Core;

public class RecentFileCleaner : IDisposable
{
    private readonly RecentFileWatcher _watcher;
    private readonly ExclusionEngine _engine;
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
    }

    public void Start()
    {
        if (_config.ScanOnStartup)
            ScanExisting();

        _watcher.Start();
    }

    public void Stop()
    {
        _watcher.Stop();
    }

    /// <summary>
    /// Scans all existing .lnk files in the Recent folder and removes matches.
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

            Log.Info($"ScanExisting complete: removed {removed} file(s)");
        }
        catch (Exception ex)
        {
            Log.Error("ScanExisting failed", ex);
        }

        return removed;
    }

    private void HandleNewRecentFile(string lnkPath)
    {
        RetryWithDelay(() => TryRemoveIfExcluded(lnkPath), maxRetries: 3, delayMs: 200);
    }

    private bool TryRemoveIfExcluded(string lnkPath)
    {
        try
        {
            if (!File.Exists(lnkPath))
                return false;

            string? target = ShortcutResolver.ResolveTarget(lnkPath);
            if (target == null)
                return false;

            if (_engine.IsExcluded(target))
            {
                File.Delete(lnkPath);
                OnFileRemoved?.Invoke(lnkPath, target);
                return true;
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
        _watcher.Dispose();
    }
}
