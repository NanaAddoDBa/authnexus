using System.Xml.Linq;

namespace AuthNexus.Architecture.Tests;

public sealed class BackendDependencyTests
{
    private static readonly Type[] ModuleMarkers =
    [
        typeof(Modules.Administration.ModuleAssemblyMarker),
        typeof(Modules.Applications.ModuleAssemblyMarker),
        typeof(Modules.Audit.ModuleAssemblyMarker),
        typeof(Modules.Authentication.ModuleAssemblyMarker),
        typeof(Modules.Identity.ModuleAssemblyMarker),
        typeof(Modules.Notifications.ModuleAssemblyMarker),
        typeof(Modules.Policies.ModuleAssemblyMarker),
        typeof(Modules.Recovery.ModuleAssemblyMarker),
        typeof(Modules.Registration.ModuleAssemblyMarker),
        typeof(Modules.Risk.ModuleAssemblyMarker),
        typeof(Modules.Sessions.ModuleAssemblyMarker),
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["AuthNexus.Api"] =
            [
                "AuthNexus.Application",
                "AuthNexus.Contracts",
                "AuthNexus.Infrastructure",
            ],
            ["AuthNexus.Application"] =
            [
                "AuthNexus.Contracts",
                "AuthNexus.Domain",
                "AuthNexus.Modules.Administration",
                "AuthNexus.Modules.Applications",
                "AuthNexus.Modules.Audit",
                "AuthNexus.Modules.Authentication",
                "AuthNexus.Modules.Identity",
                "AuthNexus.Modules.Notifications",
                "AuthNexus.Modules.Policies",
                "AuthNexus.Modules.Recovery",
                "AuthNexus.Modules.Registration",
                "AuthNexus.Modules.Risk",
                "AuthNexus.Modules.Sessions",
            ],
            ["AuthNexus.Contracts"] = [],
            ["AuthNexus.Domain"] = [],
            ["AuthNexus.Infrastructure"] =
            [
                "AuthNexus.Application",
                "AuthNexus.Contracts",
                "AuthNexus.Domain",
            ],
            ["AuthNexus.Modules.Administration"] = [],
            ["AuthNexus.Modules.Applications"] =
            [
                "AuthNexus.Domain",
            ],
            ["AuthNexus.Modules.Audit"] = [],
            ["AuthNexus.Modules.Authentication"] = [],
            ["AuthNexus.Modules.Identity"] =
            [
                "AuthNexus.Domain",
            ],
            ["AuthNexus.Modules.Notifications"] = [],
            ["AuthNexus.Modules.Policies"] = [],
            ["AuthNexus.Modules.Recovery"] = [],
            ["AuthNexus.Modules.Registration"] = [],
            ["AuthNexus.Modules.Risk"] = [],
            ["AuthNexus.Modules.Sessions"] = [],
        };

    [Fact]
    public void Compiled_module_assemblies_match_the_required_catalog()
    {
        var actualAssemblies = ModuleMarkers
            .Select(marker => marker.Assembly.GetName().Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expectedAssemblies = ExpectedProjectReferences.Keys
            .Where(name => name.StartsWith("AuthNexus.Modules.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedAssemblies, actualAssemblies);

        Assert.All(
            ModuleMarkers,
            marker => Assert.Equal(marker.Assembly.GetName().Name, marker.Namespace));
    }

    [Fact]
    public void Production_project_references_match_the_approved_graph()
    {
        var repositoryRoot = FindRepositoryRoot();
        var backendRoot = Path.Combine(repositoryRoot, "src", "backend");
        var apiProject = Path.Combine(repositoryRoot, "apps", "api", "AuthNexus.Api.csproj");

        var projectFiles = Directory
            .EnumerateFiles(backendRoot, "*.csproj", SearchOption.AllDirectories)
            .Append(apiProject)
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(path)!,
                StringComparer.Ordinal);

        Assert.Equal(
            ExpectedProjectReferences.Keys.Order(StringComparer.Ordinal),
            projectFiles.Keys.Order(StringComparer.Ordinal));

        foreach (var (projectName, expectedReferences) in ExpectedProjectReferences)
        {
            var actualReferences = ReadProjectReferences(projectFiles[projectName]);

            Assert.True(
                expectedReferences.Order(StringComparer.Ordinal).SequenceEqual(actualReferences),
                $"{projectName} references [{string.Join(", ", actualReferences)}]; " +
                $"expected [{string.Join(", ", expectedReferences.Order(StringComparer.Ordinal))}].");
        }
    }

    private static string[] ReadProjectReferences(string projectFile)
    {
        var document = XDocument.Load(projectFile);

        return document
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFileNameWithoutExtension(include)!)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AuthNexus.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate AuthNexus.sln above the architecture test output directory.");
    }
}
