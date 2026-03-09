namespace BlockFromRecent.Core;

public static class Log
{
    private static readonly string LogPath = Path.Combine(
        AppContext.BaseDirectory, "block-from-recent.log");

    private static readonly object Lock = new();

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

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
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            lock (Lock)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
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
            if (!File.Exists(LogPath)) return;
            var fi = new FileInfo(LogPath);
            if (fi.Length <= maxSizeKb * 1024) return;

            var lines = File.ReadAllLines(LogPath);
            var keep = lines.Skip(lines.Length / 2).ToArray();
            File.WriteAllLines(LogPath, keep);
        }
        catch { }
    }
}
