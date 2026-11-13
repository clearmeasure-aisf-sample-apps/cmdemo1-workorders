using System.Xml.Linq;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers;

[TestFixture]
public class DataAccessNPlusOneCanaryTests
{
    private const string HandlerPrefix = """
        using Microsoft.EntityFrameworkCore;

        namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

        public sealed class Order
        {
            public int Id { get; set; }

            public int CustomerId { get; set; }
        }

        public sealed class ShopContext : DbContext
        {
            public ShopContext(DbContextOptions<ShopContext> options)
                : base(options)
            {
            }

            public DbSet<Order> Orders => Set<Order>();
        }

        public sealed class OrderByCustomerQueryHandler
        {
            private readonly ShopContext _context;

            public OrderByCustomerQueryHandler(ShopContext context) => _context = context;

        """;

    private const string PerElementSource = HandlerPrefix + """
            public async Task<List<Order>[]> Handle(IReadOnlyList<int> customerIds)
            {
                var results = new List<List<Order>>();
                foreach (var customerId in customerIds)
                {
                    var orders = await _context.Orders
                        .Where(order => order.CustomerId == customerId)
                        .ToListAsync();
                    results.Add(orders);
                }

                return results.ToArray();
            }
        }
        """;

    private const string BatchedSource = HandlerPrefix + """
            public async Task<ILookup<int, Order>> Handle(IReadOnlyList<int> customerIds)
            {
                var orders = await _context.Orders
                    .Where(order => customerIds.Contains(order.CustomerId))
                    .ToListAsync();
                return orders.ToLookup(order => order.CustomerId);
            }
        }
        """;

    [Test]
    public async Task ShouldReportNp0001AndLc007AsErrors_When_QueryRunsOncePerElement()
    {
        var result = await BuildCanaryAsync(PerElementSource);

        var diagnostics = ReadDiagnostics(result.Output);
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == "NP0001" && diagnostic.Severity == "error",
            result.Output);
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == "LC007" && diagnostic.Severity == "error",
            result.Output);
        result.ExitCode.ShouldNotBe(0, result.Output);
    }

    [Test]
    public async Task ShouldReportNeitherNp0001NorLc007_When_QueryIsBatchedAndGrouped()
    {
        var result = await BuildCanaryAsync(BatchedSource);

        result.ExitCode.ShouldBe(0, result.Output);
        ReadDiagnostics(result.Output)
            .Where(diagnostic => diagnostic.Id is "NP0001" or "LC007")
            .ShouldBeEmpty(result.Output);
    }

    private static async Task<CommandOutcome> BuildCanaryAsync(string source)
    {
        var checkout = RepositoryCheckout.Current;
        var dataAccessProject = checkout.PathOf("src/DataAccess/DataAccess.csproj");
        var editorConfig = checkout.PathOf("src/DataAccess/.editorconfig");
        File.Exists(dataAccessProject).ShouldBeTrue(dataAccessProject);
        File.Exists(editorConfig).ShouldBeTrue(editorConfig);
        RequireErrorSeverity(editorConfig, "NP0001");
        RequireErrorSeverity(editorConfig, "LC007");

        var project = XDocument.Load(dataAccessProject);
        var centralVersions = ReadCentralVersions(XDocument.Load(checkout.PathOf("src/Directory.Packages.props")));
        using var canary = new ScratchTree("dataaccess-nplusone-canary")
            .Write(".editorconfig", await File.ReadAllTextAsync(editorConfig))
            .Write("OrderByCustomerQueryHandler.cs", source);
        WriteProject(canary.PathOf("Canary.csproj"), ReadTargetFramework(project), ReadCanaryPackages(project, centralVersions));
        return await ExternalCommand.Dotnet("build", "Canary.csproj", "--nologo", "--no-incremental")
            .In(canary.Root)
            .RunAsync();
    }

    private static void RequireErrorSeverity(string editorConfigPath, string diagnosticId)
    {
        var severity = ReadSeverity(editorConfigPath, diagnosticId);
        var key = $"dotnet_diagnostic.{diagnosticId}.severity";
        var message = string.IsNullOrEmpty(severity)
            ? $"src/DataAccess/.editorconfig does not set {key}."
            : $"src/DataAccess/.editorconfig sets {key} to '{severity}', which is below error.";
        string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(message);
    }

    private static string ReadSeverity(string editorConfigPath, string diagnosticId)
    {
        var key = $"dotnet_diagnostic.{diagnosticId}.severity";
        var severity = "";
        foreach (var raw in File.ReadLines(editorConfigPath))
        {
            var line = raw.Trim();
            if (line.StartsWith('#') || !line.StartsWith(key, StringComparison.Ordinal))
            {
                continue;
            }

            var value = line[key.Length..].Trim();
            if (value.StartsWith('='))
            {
                value = value[1..].Trim();
            }

            var comment = value.IndexOf('#', StringComparison.Ordinal);
            if (comment >= 0)
            {
                value = value[..comment].Trim();
            }

            severity = value;
        }

        return severity;
    }

    private static IReadOnlyDictionary<string, string> ReadCentralVersions(XDocument packagesProps)
    {
        return packagesProps
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageVersion" && element.Attribute("Condition") is null)
            .ToDictionary(
                element => element.Attribute("Include")?.Value.Trim() ?? "",
                element => element.Attribute("Version")?.Value.Trim() ?? "",
                StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<PackageReference> ReadCanaryPackages(
        XDocument project,
        IReadOnlyDictionary<string, string> centralVersions)
    {
        var packages = project
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => ReadPackageReference(element, centralVersions))
            .Where(reference => reference.Include.Length > 0)
            .GroupBy(reference => reference.Include, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return
        [
            RequirePackage(packages, "Microsoft.EntityFrameworkCore"),
            RequirePackage(packages, "NPlusOne.Analyzers"),
            RequirePackage(packages, "LinqContraband")
        ];
    }

    private static PackageReference RequirePackage(
        IReadOnlyDictionary<string, PackageReference> packages,
        string name)
    {
        packages.ContainsKey(name).ShouldBeTrue(
            $"DataAccess.csproj is missing PackageReference {name}.");
        var reference = packages[name];
        reference.Version.ShouldNotBeNullOrWhiteSpace(
            $"PackageReference {name} has no Version in DataAccess.csproj or src/Directory.Packages.props.");
        return reference;
    }

    private static PackageReference ReadPackageReference(
        XElement element,
        IReadOnlyDictionary<string, string> centralVersions)
    {
        var include = element.Attribute("Include")?.Value.Trim() ?? "";
        var version = element.Attribute("Version")?.Value.Trim();
        if (string.IsNullOrEmpty(version))
        {
            version = centralVersions.GetValueOrDefault(include, "");
        }

        return new PackageReference(
            include,
            version,
            ChildValue(element, "PrivateAssets"),
            ChildValue(element, "IncludeAssets"));
    }

    private static string? ChildValue(XElement element, string name)
    {
        var value = element.Elements()
            .FirstOrDefault(child => child.Name.LocalName == name)
            ?.Value
            .Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string ReadTargetFramework(XDocument project)
    {
        var targetFramework = project
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "TargetFramework")
            ?.Value
            .Trim();
        if (string.IsNullOrEmpty(targetFramework))
        {
            throw new InvalidOperationException("DataAccess.csproj has no TargetFramework.");
        }

        return targetFramework;
    }

    private static void WriteProject(
        string path,
        string targetFramework,
        IReadOnlyList<PackageReference> packages)
    {
        var itemGroup = new XElement("ItemGroup");
        foreach (var package in packages)
        {
            var element = new XElement(
                "PackageReference",
                new XAttribute("Include", package.Include),
                new XAttribute("Version", package.Version));
            if (package.PrivateAssets is { Length: > 0 })
            {
                element.Add(new XElement("PrivateAssets", package.PrivateAssets));
            }

            if (package.IncludeAssets is { Length: > 0 })
            {
                element.Add(new XElement("IncludeAssets", package.IncludeAssets));
            }

            itemGroup.Add(element);
        }

        var document = new XDocument(
            new XElement(
                "Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement(
                    "PropertyGroup",
                    new XElement("TargetFramework", targetFramework),
                    new XElement("Nullable", "enable"),
                    new XElement("ImplicitUsings", "enable"),
                    new XElement("TreatWarningsAsErrors", "false")),
                itemGroup));
        document.Save(path);
    }

    private static IReadOnlyList<AnalyzerDiagnostic> ReadDiagnostics(string output)
    {
        var found = new List<AnalyzerDiagnostic>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!raw.Contains("OrderByCustomerQueryHandler.cs(", StringComparison.Ordinal))
            {
                continue;
            }

            var diagnostic = TryReadDiagnostic(raw);
            if (diagnostic is null || !seen.Add($"{diagnostic.Severity}:{diagnostic.Id}"))
            {
                continue;
            }

            found.Add(diagnostic);
        }

        return found;
    }

    private static AnalyzerDiagnostic? TryReadDiagnostic(string line)
    {
        var marker = line.IndexOf("): ", StringComparison.Ordinal);
        if (marker < 0)
        {
            return null;
        }

        var rest = line[(marker + 3)..].TrimStart();
        var space = rest.IndexOf(' ');
        if (space <= 0)
        {
            return null;
        }

        var severity = rest[..space];
        if (severity is not ("error" or "warning" or "info"))
        {
            return null;
        }

        var idStart = space + 1;
        var colon = rest.IndexOf(':', idStart);
        if (colon <= idStart)
        {
            return null;
        }

        var id = rest[idStart..colon];
        if (id.Length == 0 || !id.All(char.IsLetterOrDigit))
        {
            return null;
        }

        return new AnalyzerDiagnostic(severity, id);
    }


    private sealed record AnalyzerDiagnostic(string Severity, string Id);

    private sealed record PackageReference(
        string Include,
        string Version,
        string? PrivateAssets,
        string? IncludeAssets);

}
