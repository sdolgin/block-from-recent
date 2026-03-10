using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BlockFromRecent.Tests;

public class VersionConsistencyTests
{
    [Fact]
    public void DirectoryBuildProps_And_InstallerIss_HaveSameVersion()
    {
        var repoRoot = FindRepoRoot();

        var propsVersion = ReadVersionFromDirectoryBuildProps(
            Path.Combine(repoRoot, "Directory.Build.props"));

        var installerVersion = ReadVersionFromInstallerIss(
            Path.Combine(repoRoot, "src", "BlockFromRecent", "installer.iss"));

        Assert.Equal(propsVersion, installerVersion);
    }

    private static string ReadVersionFromDirectoryBuildProps(string path)
    {
        var doc = XDocument.Load(path);
        var version = doc.Descendants("Version").FirstOrDefault()?.Value;
        Assert.False(string.IsNullOrWhiteSpace(version),
            "Directory.Build.props must contain a <Version> element");
        return version!;
    }

    private static string ReadVersionFromInstallerIss(string path)
    {
        var lines = File.ReadAllLines(path);
        var regex = new Regex(@"#define\s+MyAppVersion\s+""([^""]+)""");
        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
                return match.Groups[1].Value;
        }

        Assert.Fail("installer.iss must contain #define MyAppVersion \"x.x.x\"");
        return null!; // unreachable
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Directory.Build.props")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not find repo root (Directory.Build.props) from " + AppContext.BaseDirectory);
    }
}
