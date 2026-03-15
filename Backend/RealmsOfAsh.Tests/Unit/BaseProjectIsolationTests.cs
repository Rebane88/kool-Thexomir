using System.Xml.Linq;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// PROJ-02: Base project must have zero knowledge of Domain, Infrastructure, Application, or API.
/// Verified by inspecting Base.csproj (no ProjectReference) and all Base .cs source files (no forbidden usings).
/// </summary>
public class BaseProjectIsolationTests
{
    private static readonly string BaseDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Base"));

    private static readonly string[] ForbiddenNamespaces =
        ["Domain", "Infrastructure", "Application", "API"];

    [Fact]
    public void Base_csproj_has_no_ProjectReference_to_Domain_Infrastructure_Application_or_API()
    {
        var csprojPath = Path.Combine(BaseDir, "Base.csproj");
        Assert.True(File.Exists(csprojPath), $"Base.csproj not found at {csprojPath}");

        var doc = XDocument.Load(csprojPath);
        var projectReferences = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        foreach (var forbidden in ForbiddenNamespaces)
        {
            var violations = projectReferences
                .Where(r => r.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.True(
                violations.Count == 0,
                $"Base.csproj has forbidden ProjectReference to '{forbidden}': {string.Join(", ", violations)}");
        }
    }

    [Fact]
    public void Base_source_files_have_no_using_statements_referencing_Domain_Infrastructure_Application_or_API()
    {
        var csFiles = Directory.GetFiles(BaseDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        Assert.True(csFiles.Count > 0, $"No .cs files found under {BaseDir}");

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            foreach (var (line, lineNumber) in lines.Select((l, i) => (l, i + 1)))
            {
                var trimmed = line.TrimStart();
                if (!trimmed.StartsWith("using ")) continue;

                foreach (var forbidden in ForbiddenNamespaces)
                {
                    // Match "using Domain", "using Domain.X", "using Infrastructure" etc.
                    if (trimmed.StartsWith($"using {forbidden};") ||
                        trimmed.StartsWith($"using {forbidden}."))
                    {
                        violations.Add($"{Path.GetRelativePath(BaseDir, file)}:{lineNumber}: {trimmed.TrimEnd(';')}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Base project contains forbidden namespace references:\n{string.Join("\n", violations)}");
    }
}
