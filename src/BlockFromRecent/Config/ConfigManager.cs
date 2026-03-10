using System.Text.Json;
using System.Text.Json.Serialization;
using BlockFromRecent.Core;

namespace BlockFromRecent.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(RulesExport))]
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

    public static void ExportRules(List<ExclusionRule> rules, string filePath)
    {
        var export = new RulesExport { Rules = rules };
        string json = JsonSerializer.Serialize(export, AppConfigJsonContext.Default.RulesExport);
        File.WriteAllText(filePath, json);
    }

    public static RulesExport ImportRules(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var export = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.RulesExport);

        if (export?.Rules == null)
            throw new InvalidDataException("The file does not contain a valid rules list.");

        foreach (var rule in export.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern))
                throw new InvalidDataException("One or more rules have an empty pattern.");
        }

        return export;
    }
}
