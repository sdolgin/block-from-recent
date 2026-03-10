using BlockFromRecent.Config;
using BlockFromRecent.Core;

namespace BlockFromRecent.Tests;

public class RuleValidatorTests
{
    private static readonly List<ExclusionRule> EmptyRules = new();

    // --- Path prefix validation ---

    [Fact]
    public void PathPrefix_EmptyPattern_ReturnsError()
    {
        string? error = RuleValidator.Validate("", RuleType.PathPrefix, EmptyRules);

        Assert.NotNull(error);
        Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathPrefix_WhitespaceOnly_ReturnsError()
    {
        string? error = RuleValidator.Validate("   ", RuleType.PathPrefix, EmptyRules);

        Assert.NotNull(error);
        Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathPrefix_NoPathSeparator_ReturnsError()
    {
        string? error = RuleValidator.Validate("noslash", RuleType.PathPrefix, EmptyRules);

        Assert.NotNull(error);
        Assert.Contains("separator", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathPrefix_WithBackslash_ReturnsNull()
    {
        string? error = RuleValidator.Validate(@"C:\Users\Test\Downloads", RuleType.PathPrefix, EmptyRules);

        Assert.Null(error);
    }

    [Fact]
    public void PathPrefix_WithForwardSlash_ReturnsNull()
    {
        string? error = RuleValidator.Validate("C:/Users/Test/Downloads", RuleType.PathPrefix, EmptyRules);

        Assert.Null(error);
    }

    [Fact]
    public void PathPrefix_UncPath_ReturnsNull()
    {
        string? error = RuleValidator.Validate(@"\\server\share\folder", RuleType.PathPrefix, EmptyRules);

        Assert.Null(error);
    }

    [Fact]
    public void PathPrefix_DriveRoot_ReturnsWarning()
    {
        string? error = RuleValidator.Validate(@"C:\", RuleType.PathPrefix, EmptyRules);

        Assert.NotNull(error);
        Assert.Contains("drive root", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathPrefix_DriveRootForwardSlash_ReturnsWarning()
    {
        string? error = RuleValidator.Validate("D:/", RuleType.PathPrefix, EmptyRules);

        Assert.NotNull(error);
        Assert.Contains("drive root", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathPrefix_DriveRootNoTrailingSlash_ReturnsError()
    {
        string? error = RuleValidator.Validate("C:", RuleType.PathPrefix, EmptyRules);

        Assert.NotNull(error);
    }

    [Fact]
    public void PathPrefix_DriveWithSubfolder_ReturnsNull()
    {
        string? error = RuleValidator.Validate(@"C:\Users\", RuleType.PathPrefix, EmptyRules);

        Assert.Null(error);
    }

    // --- Glob pattern validation ---

    [Fact]
    public void GlobPattern_EmptyPattern_ReturnsError()
    {
        string? error = RuleValidator.Validate("", RuleType.GlobPattern, EmptyRules);

        Assert.NotNull(error);
        Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GlobPattern_ValidExtension_ReturnsNull()
    {
        string? error = RuleValidator.Validate("*.mp4", RuleType.GlobPattern, EmptyRules);

        Assert.Null(error);
    }

    [Fact]
    public void GlobPattern_ValidPathBased_ReturnsNull()
    {
        string? error = RuleValidator.Validate("**/temp/**", RuleType.GlobPattern, EmptyRules);

        Assert.Null(error);
    }

    [Fact]
    public void GlobPattern_ValidComplexPattern_ReturnsNull()
    {
        string? error = RuleValidator.Validate("**/*.{mp4,avi,mkv}", RuleType.GlobPattern, EmptyRules);

        Assert.Null(error);
    }

    // --- Duplicate detection ---

    [Fact]
    public void Duplicate_SamePatternAndType_ReturnsError()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Users\Test", Type = RuleType.PathPrefix }
        };

        string? error = RuleValidator.Validate(@"C:\Users\Test", RuleType.PathPrefix, rules);

        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_CaseInsensitive_ReturnsError()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Users\Test", Type = RuleType.PathPrefix }
        };

        string? error = RuleValidator.Validate(@"c:\users\test", RuleType.PathPrefix, rules);

        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_DifferentType_ReturnsNull()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Users\Test", Type = RuleType.PathPrefix }
        };

        // Same pattern but as GlobPattern type — not a duplicate
        // (Though this glob is odd, it's technically a different rule type)
        string? error = RuleValidator.Validate(@"C:\Users\Test", RuleType.GlobPattern, rules);

        // Should not report duplicate — different types
        Assert.Null(error);
    }

    [Fact]
    public void Duplicate_EditSameIndex_ReturnsNull()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Users\Test", Type = RuleType.PathPrefix }
        };

        // Editing rule at index 0 with same pattern — not a duplicate
        string? error = RuleValidator.Validate(@"C:\Users\Test", RuleType.PathPrefix, rules, editIndex: 0);

        Assert.Null(error);
    }

    [Fact]
    public void Duplicate_EditDifferentIndex_ReturnsError()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Users\Test", Type = RuleType.PathPrefix },
            new() { Pattern = @"D:\Other", Type = RuleType.PathPrefix }
        };

        // Editing rule at index 1 to same pattern as index 0 — duplicate
        string? error = RuleValidator.Validate(@"C:\Users\Test", RuleType.PathPrefix, rules, editIndex: 1);

        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_GlobPattern_ReturnsError()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = "*.mp4", Type = RuleType.GlobPattern }
        };

        string? error = RuleValidator.Validate("*.mp4", RuleType.GlobPattern, rules);

        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_TrimmedMatch_ReturnsError()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = "*.mp4", Type = RuleType.GlobPattern }
        };

        string? error = RuleValidator.Validate("  *.mp4  ", RuleType.GlobPattern, rules);

        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
    }

    // --- Valid patterns should pass ---

    [Fact]
    public void ValidPathPrefix_WithExistingRules_ReturnsNull()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = @"C:\Users\Test", Type = RuleType.PathPrefix }
        };

        string? error = RuleValidator.Validate(@"D:\Other\Path", RuleType.PathPrefix, rules);

        Assert.Null(error);
    }

    [Fact]
    public void ValidGlobPattern_WithExistingRules_ReturnsNull()
    {
        var rules = new List<ExclusionRule>
        {
            new() { Pattern = "*.mp4", Type = RuleType.GlobPattern }
        };

        string? error = RuleValidator.Validate("*.avi", RuleType.GlobPattern, rules);

        Assert.Null(error);
    }
}
