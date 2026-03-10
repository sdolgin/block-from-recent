namespace BlockFromRecent.Config;

public enum RuleType
{
    PathPrefix,
    GlobPattern
}

public class ExclusionRule
{
    public string Pattern { get; set; } = string.Empty;
    public RuleType Type { get; set; } = RuleType.PathPrefix;

    public override string ToString()
    {
        string prefix = Type == RuleType.PathPrefix ? "[Path] " : "[Glob] ";
        return prefix + Pattern;
    }
}

public class AppConfig
{
    public List<ExclusionRule> Rules { get; set; } = new();
    public bool AutoStart { get; set; } = false;
    public bool ScanOnStartup { get; set; } = true;
    public bool VerboseLogging { get; set; } = false;
    public int PeriodicScanIntervalMinutes { get; set; } = 5;
}
