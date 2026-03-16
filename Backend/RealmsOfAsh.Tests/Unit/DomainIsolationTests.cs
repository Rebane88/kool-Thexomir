using System.Xml.Linq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// ARCH-01: Domain project must have zero external PackageReference dependencies.
/// ARCH-02: Domain source files must not reference Infrastructure, Application, or API namespaces.
/// </summary>
public class DomainIsolationTests
{
    private static readonly string DomainDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Domain"));

    private static readonly string[] ForbiddenNamespaces =
        ["Infrastructure", "Application", "API", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore"];

    [Fact]
    public void Domain_csproj_has_no_PackageReference()
    {
        var csprojPath = Path.Combine(DomainDir, "Domain.csproj");
        File.Exists(csprojPath).ShouldBeTrue($"Domain.csproj not found at {csprojPath}");

        var doc = XDocument.Load(csprojPath);
        var packageReferences = doc.Descendants("PackageReference").ToList();

        packageReferences.Count.ShouldBe(0,
            $"Domain.csproj has PackageReference(s): {string.Join(", ", packageReferences.Select(p => p.Attribute("Include")?.Value))}");
    }

    [Fact]
    public void Domain_csproj_has_no_ProjectReference_to_Infrastructure_Application_or_API()
    {
        var csprojPath = Path.Combine(DomainDir, "Domain.csproj");
        File.Exists(csprojPath).ShouldBeTrue($"Domain.csproj not found at {csprojPath}");

        var doc = XDocument.Load(csprojPath);
        var projectReferences = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        var forbidden = new[] { "Infrastructure", "Application", "API" };
        foreach (var ns in forbidden)
        {
            var violations = projectReferences
                .Where(r => r.Contains(ns, StringComparison.OrdinalIgnoreCase))
                .ToList();

            violations.Count.ShouldBe(0,
                $"Domain.csproj has forbidden ProjectReference to '{ns}': {string.Join(", ", violations)}");
        }
    }

    [Fact]
    public void Domain_source_files_have_no_forbidden_using_statements()
    {
        var csFiles = Directory.GetFiles(DomainDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        csFiles.Count.ShouldBeGreaterThan(0, $"No .cs files found under {DomainDir}");

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
                    if (trimmed.StartsWith($"using {forbidden};") ||
                        trimmed.StartsWith($"using {forbidden}."))
                    {
                        violations.Add($"{Path.GetRelativePath(DomainDir, file)}:{lineNumber}: {trimmed.TrimEnd(';')}");
                    }
                }
            }
        }

        violations.Count.ShouldBe(0,
            $"Domain project contains forbidden namespace references:\n{string.Join("\n", violations)}");
    }
}
