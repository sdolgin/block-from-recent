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

        // Clean up any leftover corrupt backup from previous tests
        if (File.Exists(AppPaths.CorruptConfigBackupFile))
            File.Delete(AppPaths.CorruptConfigBackupFile);
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

        // Clean up corrupt backup created by tests
        if (File.Exists(AppPaths.CorruptConfigBackupFile))
            File.Delete(AppPaths.CorruptConfigBackupFile);
    }

    [Fact]
    public void SaveThenLoad_ProducesIdenticalConfig()
    {
        var original = new AppConfig
        {
            AutoStart = true,
            ScanOnStartup = false,
            VerboseLogging = true,
            Rules = new List<ExclusionRule>
            {
                new() { Pattern = @"C:\Temp", Type = RuleType.PathPrefix },
                new() { Pattern = "*.log", Type = RuleType.GlobPattern }
            }
        };

        ConfigManager.Save(original);
        var (loaded, wasCorrupted) = ConfigManager.Load();

        Assert.False(wasCorrupted);
        Assert.Equal(original.AutoStart, loaded.AutoStart);
        Assert.Equal(original.ScanOnStartup, loaded.ScanOnStartup);
        Assert.Equal(original.VerboseLogging, loaded.VerboseLogging);
        Assert.Equal(original.Rules.Count, loaded.Rules.Count);

        for (int i = 0; i < original.Rules.Count; i++)
        {
            Assert.Equal(original.Rules[i].Pattern, loaded.Rules[i].Pattern);
            Assert.Equal(original.Rules[i].Type, loaded.Rules[i].Type);
        }
    }

    [Fact]
    public void WhenConfigFileIsCorrupted_ThenReturnsDefaultConfigAndSignalsCorruption()
    {
        AppPaths.EnsureCreated();
        File.WriteAllText(AppPaths.ConfigFile, "NOT VALID JSON {{{");

        var (config, wasCorrupted) = ConfigManager.Load();

        Assert.True(wasCorrupted);
        Assert.NotNull(config);
        Assert.Empty(config.Rules);
        Assert.False(config.AutoStart);
        Assert.True(config.ScanOnStartup);
        Assert.False(config.VerboseLogging);
    }

    [Fact]
    public void WhenConfigFileIsCorrupted_ThenBackupFileIsCreated()
    {
        AppPaths.EnsureCreated();
        string corruptContent = "NOT VALID JSON {{{";
        File.WriteAllText(AppPaths.ConfigFile, corruptContent);

        ConfigManager.Load();

        Assert.True(File.Exists(AppPaths.CorruptConfigBackupFile));
        Assert.Equal(corruptContent, File.ReadAllText(AppPaths.CorruptConfigBackupFile));
    }

    [Fact]
    public void WhenConfigFileDoesNotExist_ThenReturnsDefaultConfigWithNoCorruption()
    {
        if (File.Exists(AppPaths.ConfigFile))
            File.Delete(AppPaths.ConfigFile);

        var (config, wasCorrupted) = ConfigManager.Load();

        Assert.False(wasCorrupted);
        Assert.NotNull(config);
        Assert.Empty(config.Rules);
    }
}
