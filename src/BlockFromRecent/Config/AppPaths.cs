namespace BlockFromRecent.Config;

/// <summary>
/// Central location for all app data paths.
/// Uses %AppData%\BlockFromRecent\ so the app can write freely
/// even when installed to Program Files.
/// </summary>
public static class AppPaths
{
    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BlockFromRecent");

    public static string ConfigFile => Path.Combine(DataDir, "config.json");
    public static string CorruptConfigBackupFile => Path.Combine(DataDir, "config.json.corrupt");
    public static string LogFile => Path.Combine(DataDir, "block-from-recent.log");

    /// <summary>
    /// Ensures the data directory exists. Call once at startup.
    /// </summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDir);
    }
}
