using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlockFromRecent.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
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
}
