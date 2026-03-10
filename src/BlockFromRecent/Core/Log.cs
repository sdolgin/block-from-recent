using BlockFromRecent.Config;

namespace BlockFromRecent.Core;

public static class Log
{
    private static readonly object Lock = new();
    private static bool _verbose;

    public static bool Verbose
    {
        get => _verbose;
        set => _verbose = value;
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    /// <summary>
    /// Debug-level logging — only written when VerboseLogging is enabled.
    /// </summary>
    public static void Debug(string message)
    {
        if (_verbose)
            Write("DEBUG", message);
    }

    public static void Error(string message, Exception ex)
    {
        Write("ERROR", $"{message}: {ex.GetType().Name}: {ex.Message}");
        Write("ERROR", $"  StackTrace: {ex.StackTrace}");
        if (ex.InnerException != null)
            Write("ERROR", $"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            AppPaths.EnsureCreated();
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            lock (Lock)
            {
                File.AppendAllText(AppPaths.LogFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Can't log — don't crash
        }
    }

    /// <summary>
    /// Trims the log file if it exceeds maxSizeKb.
    /// Keeps only the last half of lines.
    /// </summary>
    public static void TrimIfNeeded(int maxSizeKb = 512)
    {
        try
        {
            string logPath = AppPaths.LogFile;
            if (!File.Exists(logPath)) return;
            var fi = new FileInfo(logPath);
            if (fi.Length <= maxSizeKb * 1024) return;

            var lines = File.ReadAllLines(logPath);
            var keep = lines.Skip(lines.Length / 2).ToArray();
            File.WriteAllLines(logPath, keep);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log.TrimIfNeeded failed: {ex.Message}");
        }
    }
}
