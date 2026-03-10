namespace BlockFromRecent.Core;

public class RecentFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly System.Timers.Timer _debounceTimer;
    private readonly HashSet<string> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event Func<string, Task>? OnNewRecentFile;

    public static string RecentFolderPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent));

    public RecentFileWatcher()
    {
        _watcher = new FileSystemWatcher(RecentFolderPath, "*.lnk")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = false
        };

        _watcher.Created += OnFileCreated;
        _watcher.Renamed += OnFileRenamed;

        // Debounce timer — process pending files after 500ms of quiet
        _debounceTimer = new System.Timers.Timer(500) { AutoReset = false };
        _debounceTimer.Elapsed += async (_, _) =>
        {
            try
            {
                await ProcessPendingFilesAsync();
            }
            catch (Exception ex)
            {
                Log.Error("ProcessPendingFiles failed", ex);
            }
        };
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        EnqueueFile(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.FullPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            EnqueueFile(e.FullPath);
    }

    private void EnqueueFile(string path)
    {
        Log.Debug($"Watcher event: {Path.GetFileName(path)}");
        lock (_lock)
        {
            _pendingFiles.Add(path);
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    private async Task ProcessPendingFilesAsync()
    {
        string[] files;
        lock (_lock)
        {
            files = _pendingFiles.ToArray();
            _pendingFiles.Clear();
        }

        foreach (var file in files)
        {
            var handler = OnNewRecentFile;
            if (handler != null)
                await handler(file);
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}
