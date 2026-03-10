using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlockFromRecent.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(RulesExport))]
internal partial class AppConfigJsonContext : JsonSerializerContext { }

public static class ConfigManager
{
    public static AppConfig Load()
    {
        string configPath = AppPaths.ConfigFile;

        if (!File.Exists(configPath))
        {
            var defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            string json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        AppPaths.EnsureCreated();
        string json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(AppPaths.ConfigFile, json);
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
