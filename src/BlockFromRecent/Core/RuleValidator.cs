using BlockFromRecent.Config;
using Microsoft.Extensions.FileSystemGlobbing;

namespace BlockFromRecent.Core;

/// <summary>
/// Validates exclusion rule patterns before they are saved.
/// </summary>
public static class RuleValidator
{
    /// <summary>
    /// Validates a rule pattern and returns an error message, or null if valid.
    /// </summary>
    /// <param name="pattern">The pattern text entered by the user.</param>
    /// <param name="type">The rule type (PathPrefix or GlobPattern).</param>
    /// <param name="existingRules">Current list of rules to check for duplicates.</param>
    /// <param name="editIndex">If editing an existing rule, its index (to exclude from duplicate check).</param>
    public static string? Validate(string pattern, RuleType type, IReadOnlyList<ExclusionRule> existingRules, int? editIndex = null)
    {
        string trimmed = pattern.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return "Pattern cannot be empty.";

        // Check for duplicates
        for (int i = 0; i < existingRules.Count; i++)
        {
            if (i == editIndex)
                continue;

            var existing = existingRules[i];
            if (existing.Type == type &&
                string.Equals(existing.Pattern.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return "A rule with this pattern and type already exists.";
            }
        }

        return type switch
        {
            RuleType.PathPrefix => ValidatePathPrefix(trimmed),
            RuleType.GlobPattern => ValidateGlobPattern(trimmed),
            _ => null
        };
    }

    private static string? ValidatePathPrefix(string pattern)
    {
        // Must contain at least one path separator
        if (!pattern.Contains('\\') && !pattern.Contains('/'))
            return "Path prefix must contain at least one path separator (\\ or /).";

        // Warn about drive root patterns (e.g., "C:\", "D:/")
        string normalized = pattern.Replace('/', '\\').TrimEnd('\\');
        if (normalized.Length == 2 && char.IsLetter(normalized[0]) && normalized[1] == ':')
            return "Warning: this pattern matches an entire drive root and would exclude all files on that drive.";

        return null;
    }

    private static string? ValidateGlobPattern(string pattern)
    {
        try
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(pattern.Replace('\\', '/'));
            // Run a match to surface any deferred parsing errors
            matcher.Match("test/file.txt");
            return null;
        }
        catch (Exception ex)
        {
            return $"Invalid glob pattern: {ex.Message}";
        }
    }
}
