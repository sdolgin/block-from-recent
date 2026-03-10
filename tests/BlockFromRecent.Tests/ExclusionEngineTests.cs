using BlockFromRecent.Config;
using BlockFromRecent.Core;

namespace BlockFromRecent.Tests;

public class ExclusionEngineTests
{
    private readonly ExclusionEngine _engine = new();

    // --- Path prefix matching ---

    [Fact]
    public void PathPrefix_ExactMatch_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:\Users\Test\Downloads") });

        Assert.True(_engine.IsExcluded(@"C:\Users\Test\Downloads\file.txt"));
    }

    [Fact]
    public void PathPrefix_CaseInsensitive_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:\Users\Test\Downloads") });

        Assert.True(_engine.IsExcluded(@"c:\users\test\downloads\file.txt"));
    }

    [Fact]
    public void PathPrefix_TrailingSlashNormalized_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:\Users\Test\Downloads\") });

        Assert.True(_engine.IsExcluded(@"C:\Users\Test\Downloads\file.txt"));
    }

    [Fact]
    public void PathPrefix_ForwardSlashNormalized_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:/Users/Test/Downloads") });

        Assert.True(_engine.IsExcluded(@"C:\Users\Test\Downloads\file.txt"));
    }

    [Fact]
    public void PathPrefix_NoMatch_ReturnsFalse()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:\Users\Test\Downloads") });

        Assert.False(_engine.IsExcluded(@"C:\Users\Test\Documents\file.txt"));
    }

    // --- Glob pattern matching ---

    [Fact]
    public void GlobPattern_ExtensionMatch_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { GlobRule("*.mp4") });

        Assert.True(_engine.IsExcluded(@"C:\Videos\movie.mp4"));
    }

    [Fact]
    public void GlobPattern_ExtensionCaseInsensitive_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { GlobRule("*.MP4") });

        Assert.True(_engine.IsExcluded(@"C:\Videos\movie.mp4"));
    }

    [Fact]
    public void GlobPattern_ExtensionNoMatch_ReturnsFalse()
    {
        _engine.UpdateRules(new[] { GlobRule("*.mp4") });

        Assert.False(_engine.IsExcluded(@"C:\Videos\movie.avi"));
    }

    [Fact]
    public void GlobPattern_PathBased_ReturnsTrue()
    {
        _engine.UpdateRules(new[] { GlobRule("**/temp/**") });

        Assert.True(_engine.IsExcluded(@"Users\temp\file.txt"));
    }

    // --- Empty / null pattern handling ---

    [Fact]
    public void EmptyPattern_IsSkipped_ReturnsFalse()
    {
        _engine.UpdateRules(new[]
        {
            new ExclusionRule { Pattern = "", Type = RuleType.PathPrefix }
        });

        Assert.False(_engine.IsExcluded(@"C:\file.txt"));
    }

    [Fact]
    public void WhitespacePattern_IsSkipped_ReturnsFalse()
    {
        _engine.UpdateRules(new[]
        {
            new ExclusionRule { Pattern = "   ", Type = RuleType.GlobPattern }
        });

        Assert.False(_engine.IsExcluded(@"C:\file.txt"));
    }

    [Fact]
    public void EmptyTargetPath_ReturnsFalse()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:\Anything") });

        Assert.False(_engine.IsExcluded(""));
    }

    [Fact]
    public void WhitespaceTargetPath_ReturnsFalse()
    {
        _engine.UpdateRules(new[] { PathRule(@"C:\Anything") });

        Assert.False(_engine.IsExcluded("   "));
    }

    // --- Empty rule set ---

    [Fact]
    public void NoRules_ReturnsFalse()
    {
        _engine.UpdateRules(Array.Empty<ExclusionRule>());

        Assert.False(_engine.IsExcluded(@"C:\Users\Test\file.txt"));
    }

    // --- Helpers ---

    private static ExclusionRule PathRule(string pattern) =>
        new() { Pattern = pattern, Type = RuleType.PathPrefix };

    private static ExclusionRule GlobRule(string pattern) =>
        new() { Pattern = pattern, Type = RuleType.GlobPattern };
}
