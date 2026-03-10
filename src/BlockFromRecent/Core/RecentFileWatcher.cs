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
        _watcher.Error += OnWatcherError;

        // Debounce timer — process pending files after 500ms of quiet
        _debounceTimer = new System.Timers.Timer(500) { AutoReset = false };
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Log.Warn($"FileSystemWatcher error (possible buffer overflow): {e.GetException().Message}");
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

    private void OnDebounceTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _ = ProcessPendingFilesAsync().ContinueWith(
            t => Log.Error("ProcessPendingFiles failed", t.Exception!.InnerException ?? t.Exception),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ProcessPendingFilesAsync()
    {
        string[] files;
        lock (_lock)
        {
            files = _pendingFiles.ToArray();
            _pendingFiles.Clear();
        }

        var handler = OnNewRecentFile;
        if (handler == null)
            return;

        foreach (var file in files)
        {
            foreach (var d in handler.GetInvocationList())
            {
                await ((Func<string, Task>)d)(file);
            }
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}
