using BlockFromRecent.Config;
using Microsoft.Extensions.FileSystemGlobbing;

namespace BlockFromRecent.Core;

public class ExclusionEngine
{
    private volatile ExclusionRule[] _rules = [];

    public void UpdateRules(IEnumerable<ExclusionRule> rules)
    {
        _rules = rules.ToArray();
    }

    /// <summary>
    /// Checks if a target path should be excluded based on the configured rules.
    /// </summary>
    public bool IsExcluded(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            return false;

        var rules = _rules;
        string normalized = NormalizePath(targetPath);
        Log.Debug($"ExclusionEngine: checking \"{normalized}\" against {rules.Length} rule(s)");

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern))
                continue;

            switch (rule.Type)
            {
                case RuleType.PathPrefix:
                    string normalizedPrefix = NormalizePath(rule.Pattern);
                    bool prefixMatch = normalized.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
                    Log.Debug($"  PathPrefix \"{normalizedPrefix}\" -> {(prefixMatch ? "MATCH" : "no match")}");
                    if (prefixMatch)
                        return true;
                    break;

                case RuleType.GlobPattern:
                    bool globMatch = MatchesGlob(normalized, rule.Pattern);
                    Log.Debug($"  GlobPattern \"{rule.Pattern}\" -> {(globMatch ? "MATCH" : "no match")}");
                    if (globMatch)
                        return true;
                    break;
            }
        }

        Log.Debug($"  Result: NOT excluded");
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
