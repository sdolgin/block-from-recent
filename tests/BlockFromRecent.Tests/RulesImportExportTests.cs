using BlockFromRecent.Config;

namespace BlockFromRecent.Tests;

public class RulesImportExportTests : IDisposable
{
    private readonly string _tempDir;

    public RulesImportExportTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "BlockFromRecent.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ExportThenImport_ProducesIdenticalRules()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"\\server\share\", Type = RuleType.PathPrefix },
            new() { Pattern = "*.mp4", Type = RuleType.GlobPattern }
        };

        string filePath = Path.Combine(_tempDir, "rules.json");
        ConfigManager.ExportRules(rules, filePath);
        var imported = ConfigManager.ImportRules(filePath);

        Assert.Equal(rules.Count, imported.Rules.Count);
        for (int i = 0; i < rules.Count; i++)
        {
            Assert.Equal(rules[i].Pattern, imported.Rules[i].Pattern);
            Assert.Equal(rules[i].Type, imported.Rules[i].Type);
        }
    }

    [Fact]
    public void ExportRules_WritesOnlyRulesNotAppSettings()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Temp", Type = RuleType.PathPrefix }
        };

        string filePath = Path.Combine(_tempDir, "rules.json");
        ConfigManager.ExportRules(rules, filePath);

        string json = File.ReadAllText(filePath);
        Assert.Contains("Rules", json);
        Assert.DoesNotContain("AutoStart", json);
        Assert.DoesNotContain("ScanOnStartup", json);
        Assert.DoesNotContain("VerboseLogging", json);
    }

    [Fact]
    public void ImportRules_WithInvalidJson_ThrowsException()
    {
        string filePath = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(filePath, "not valid json");

        Assert.ThrowsAny<Exception>(() => ConfigManager.ImportRules(filePath));
    }

    [Fact]
    public void ImportRules_WithEmptyPattern_ThrowsInvalidDataException()
    {
        string filePath = Path.Combine(_tempDir, "empty-pattern.json");
        File.WriteAllText(filePath, """
        {
            "Rules": [
                { "Pattern": "", "Type": "PathPrefix" }
            ]
        }
        """);

        Assert.Throws<InvalidDataException>(() => ConfigManager.ImportRules(filePath));
    }

    [Fact]
    public void ImportRules_WithNullRules_ThrowsInvalidDataException()
    {
        string filePath = Path.Combine(_tempDir, "no-rules.json");
        File.WriteAllText(filePath, "{}");

        // Deserializing {} produces a RulesExport with a default empty list, which is valid
        // but a file with "Rules": null should throw
        File.WriteAllText(filePath, """{ "Rules": null }""");
        Assert.Throws<InvalidDataException>(() => ConfigManager.ImportRules(filePath));
    }

    [Fact]
    public void ImportRules_WithEmptyRulesList_ReturnsEmptyList()
    {
        string filePath = Path.Combine(_tempDir, "empty-rules.json");
        File.WriteAllText(filePath, """{ "Rules": [] }""");

        var imported = ConfigManager.ImportRules(filePath);
        Assert.Empty(imported.Rules);
    }

    [Fact]
    public void ExportRules_WithEmptyList_WritesValidJson()
    {
        string filePath = Path.Combine(_tempDir, "empty.json");
        ConfigManager.ExportRules(new List<ExclusionRule>(), filePath);

        var imported = ConfigManager.ImportRules(filePath);
        Assert.Empty(imported.Rules);
    }

    [Fact]
    public void ImportRules_WithFileNotFound_ThrowsFileNotFoundException()
    {
        string filePath = Path.Combine(_tempDir, "nonexistent.json");
        Assert.Throws<FileNotFoundException>(() => ConfigManager.ImportRules(filePath));
    }
}
