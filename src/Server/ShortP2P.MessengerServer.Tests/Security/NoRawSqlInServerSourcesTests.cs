using System.Text.RegularExpressions;

namespace ShortP2P.MessengerServer.Tests.Security;

/// <summary>
/// Guards the SQL-injection invariant: Messenger Server persistence must stay on parameterized EF LINQ.
/// </summary>
public class NoRawSqlInServerSourcesTests
{
    private static readonly Regex Forbidden = new(
        @"\b(FromSql(?:Raw|Interpolated)?|ExecuteSql(?:Raw|Interpolated)?|SqlQuery(?:Raw)?)\b|\$""[^""]*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void ServerSources_DoNotContainRawSqlApis()
    {
        var root = FindServerRoot();
        Assert.True(Directory.Exists(root), $"Server root not found: {root}");

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // This test file documents the forbidden APIs by name.
            if (file.EndsWith("NoRawSqlInServerSourcesTests.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var text = File.ReadAllText(file);
            if (Forbidden.IsMatch(text))
                offenders.Add(Path.GetRelativePath(root, file));
        }

        Assert.True(
            offenders.Count == 0,
            "Raw SQL APIs or concatenated SQL found in:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static string FindServerRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Server");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Server"));
    }
}
