using System.Text.Json;
using System.Text.Json.Serialization;
using BlockFromRecent.Core;

namespace BlockFromRecent.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext { }

public static class ConfigManager
{
    public static (AppConfig Config, bool WasCorrupted) Load()
    {
        string configPath = AppPaths.ConfigFile;

        if (!File.Exists(configPath))
        {
            var defaultConfig = new AppConfig();
            Save(defaultConfig);
            return (defaultConfig, false);
        }

        try
        {
            string json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? new AppConfig();
            return (config, false);
        }
        catch (Exception ex)
        {
            Log.Warn($"Config file is corrupted or unreadable, resetting to defaults: {ex.GetType().Name}: {ex.Message}");

            BackupCorruptedConfig(configPath);

            return (new AppConfig(), true);
        }
    }

    public static void Save(AppConfig config)
    {
        AppPaths.EnsureCreated();
        string json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(AppPaths.ConfigFile, json);
    }

    private static void BackupCorruptedConfig(string configPath)
    {
        try
        {
            string backupPath = AppPaths.CorruptConfigBackupFile;
            File.Copy(configPath, backupPath, overwrite: true);
            Log.Info($"Corrupted config backed up to {backupPath}");
        }
        catch (Exception backupEx)
        {
            Log.Warn($"Failed to back up corrupted config: {backupEx.GetType().Name}: {backupEx.Message}");
        }
    }
}
