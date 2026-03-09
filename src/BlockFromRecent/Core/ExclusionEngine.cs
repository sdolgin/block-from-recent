using BlockFromRecent.Config;
using Microsoft.Extensions.FileSystemGlobbing;

namespace BlockFromRecent.Core;

public class ExclusionEngine
{
    private List<ExclusionRule> _rules = new();

    public void UpdateRules(IEnumerable<ExclusionRule> rules)
    {
        _rules = rules.ToList();
    }

    /// <summary>
    /// Checks if a target path should be excluded based on the configured rules.
    /// </summary>
    public bool IsExcluded(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            return false;

        string normalized = NormalizePath(targetPath);

        foreach (var rule in _rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern))
                continue;

            switch (rule.Type)
            {
                case RuleType.PathPrefix:
                    string normalizedPrefix = NormalizePath(rule.Pattern);
                    if (normalized.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                    break;

                case RuleType.GlobPattern:
                    if (MatchesGlob(normalized, rule.Pattern))
                        return true;
                    break;
            }
        }

        return false;
    }

    private static bool MatchesGlob(string normalizedPath, string pattern)
    {
        // For simple extension patterns like "*.mp4", match against the filename
        if (pattern.StartsWith("*.") && !pattern.Contains('/') && !pattern.Contains('\\'))
        {
            string fileName = Path.GetFileName(normalizedPath);
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(pattern);
            return matcher.Match(fileName).HasMatches;
        }

        // For path-based globs, match against the full path
        var fullMatcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        fullMatcher.AddInclude(pattern.Replace('\\', '/'));
        return fullMatcher.Match(normalizedPath.Replace('\\', '/')).HasMatches;
    }

    private static string NormalizePath(string path)
    {
        // Normalize to backslash and trim trailing separators
        return path.Replace('/', '\\').TrimEnd('\\');
    }
}
