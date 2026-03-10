using BlockFromRecent.Config;

namespace BlockFromRecent.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly string _backupPath;
    private readonly bool _hadExistingConfig;

    public ConfigManagerTests()
    {
        // Back up any existing config to avoid interfering with real data
        _backupPath = AppPaths.ConfigFile + ".test-backup";
        _hadExistingConfig = File.Exists(AppPaths.ConfigFile);

        if (_hadExistingConfig)
            File.Copy(AppPaths.ConfigFile, _backupPath, overwrite: true);
    }

    public void Dispose()
    {
        // Restore original config (or remove if there wasn't one)
        if (_hadExistingConfig && File.Exists(_backupPath))
        {
            File.Copy(_backupPath, AppPaths.ConfigFile, overwrite: true);
            File.Delete(_backupPath);
        }
        else if (!_hadExistingConfig && File.Exists(AppPaths.ConfigFile))
        {
            File.Delete(AppPaths.ConfigFile);
        }

        if (File.Exists(_backupPath))
            File.Delete(_backupPath);
    }

    [Fact]
    public void SaveThenLoad_ProducesIdenticalConfig()
    {
        var original = new AppConfig
        {
            AutoStart = true,
            ScanOnStartup = false,
            VerboseLogging = true,
            PeriodicScanIntervalMinutes = 10,
            Rules = new List<ExclusionRule>
            {
                new() { Pattern = @"C:\Temp", Type = RuleType.PathPrefix },
                new() { Pattern = "*.log", Type = RuleType.GlobPattern }
            }
        };

        ConfigManager.Save(original);
        var loaded = ConfigManager.Load();

        Assert.Equal(original.AutoStart, loaded.AutoStart);
        Assert.Equal(original.ScanOnStartup, loaded.ScanOnStartup);
        Assert.Equal(original.VerboseLogging, loaded.VerboseLogging);
        Assert.Equal(original.PeriodicScanIntervalMinutes, loaded.PeriodicScanIntervalMinutes);
        Assert.Equal(original.Rules.Count, loaded.Rules.Count);

        for (int i = 0; i < original.Rules.Count; i++)
        {
            Assert.Equal(original.Rules[i].Pattern, loaded.Rules[i].Pattern);
            Assert.Equal(original.Rules[i].Type, loaded.Rules[i].Type);
        }
    }
}
