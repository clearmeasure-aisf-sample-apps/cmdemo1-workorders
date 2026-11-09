using System.Diagnostics;
using System.Xml.Linq;
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

    private static async Task<BuildResult> BuildCanaryAsync(string source)
    {
        string repoRoot = FindRepoRoot();
        string dataAccessProject = Path.Combine(repoRoot, "src", "DataAccess", "DataAccess.csproj");
        string editorConfig = Path.Combine(repoRoot, "src", "DataAccess", ".editorconfig");
        File.Exists(dataAccessProject).ShouldBeTrue(dataAccessProject);
        File.Exists(editorConfig).ShouldBeTrue(editorConfig);
        RequireErrorSeverity(editorConfig, "NP0001");
        RequireErrorSeverity(editorConfig, "LC007");

        var project = XDocument.Load(dataAccessProject);
        var packages = ReadCanaryPackages(project);
        string targetFramework = ReadTargetFramework(project);
        string projectDirectory = Path.Combine(
            Path.GetTempPath(),
            $"dataaccess-nplusone-canary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectDirectory);

        try
        {
            File.Copy(editorConfig, Path.Combine(projectDirectory, ".editorconfig"));
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "OrderByCustomerQueryHandler.cs"),
                source);
            WriteProject(
                Path.Combine(projectDirectory, "Canary.csproj"),
                targetFramework,
                packages);
            return await RunDotNetBuild(projectDirectory);
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    private static void RequireErrorSeverity(string editorConfigPath, string diagnosticId)
    {
        string severity = ReadSeverity(editorConfigPath, diagnosticId);
        string key = $"dotnet_diagnostic.{diagnosticId}.severity";
        string message = string.IsNullOrEmpty(severity)
            ? $"src/DataAccess/.editorconfig does not set {key}."
            : $"src/DataAccess/.editorconfig sets {key} to '{severity}', which is below error.";
        string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(message);
    }

    private static string ReadSeverity(string editorConfigPath, string diagnosticId)
    {
        string key = $"dotnet_diagnostic.{diagnosticId}.severity";
        string severity = "";
        foreach (string raw in File.ReadLines(editorConfigPath))
        {
            string line = raw.Trim();
            if (line.StartsWith('#') || !line.StartsWith(key, StringComparison.Ordinal))
            {
                continue;
            }

            string value = line[key.Length..].Trim();
            if (value.StartsWith('='))
            {
                value = value[1..].Trim();
            }

            int comment = value.IndexOf('#', StringComparison.Ordinal);
            if (comment >= 0)
            {
                value = value[..comment].Trim();
            }

            severity = value;
        }

        return severity;
    }

    private static IReadOnlyList<PackageReference> ReadCanaryPackages(XDocument project)
    {
        var packages = project
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(ReadPackageReference)
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
            $"DataAccess.csproj PackageReference {name} has no Version.");
        return reference;
    }

    private static PackageReference ReadPackageReference(XElement element)
    {
        string include = element.Attribute("Include")?.Value.Trim() ?? "";
        string? version = element.Attribute("Version")?.Value.Trim();
        if (string.IsNullOrEmpty(version))
        {
            version = element.Elements()
                .FirstOrDefault(child => child.Name.LocalName == "Version")
                ?.Value
                .Trim() ?? "";
        }

        return new PackageReference(
            include,
            version,
            ChildValue(element, "PrivateAssets"),
            ChildValue(element, "IncludeAssets"));
    }

    private static string? ChildValue(XElement element, string name)
    {
        string? value = element.Elements()
            .FirstOrDefault(child => child.Name.LocalName == name)
            ?.Value
            .Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string ReadTargetFramework(XDocument project)
    {
        string? targetFramework = project
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

    private static async Task<BuildResult> RunDotNetBuild(string workingDirectory)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("Canary.csproj");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--no-incremental");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet build.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            string partial = await CombineOutput(standardOutput, standardError);
            throw new TimeoutException(
                $"dotnet build did not finish within 4 minutes.{Environment.NewLine}{partial}");
        }

        return new BuildResult(process.ExitCode, await CombineOutput(standardOutput, standardError));
    }

    private static async Task<string> CombineOutput(Task<string> standardOutput, Task<string> standardError)
    {
        return $"{await standardOutput}{Environment.NewLine}{await standardError}";
    }

    private static IReadOnlyList<AnalyzerDiagnostic> ReadDiagnostics(string output)
    {
        var found = new List<AnalyzerDiagnostic>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
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
        int marker = line.IndexOf("): ", StringComparison.Ordinal);
        if (marker < 0)
        {
            return null;
        }

        string rest = line[(marker + 3)..].TrimStart();
        int space = rest.IndexOf(' ');
        if (space <= 0)
        {
            return null;
        }

        string severity = rest[..space];
        if (severity is not ("error" or "warning" or "info"))
        {
            return null;
        }

        int idStart = space + 1;
        int colon = rest.IndexOf(':', idStart);
        if (colon <= idStart)
        {
            return null;
        }

        string id = rest[idStart..colon];
        if (id.Length == 0 || !id.All(char.IsLetterOrDigit))
        {
            return null;
        }

        return new AnalyzerDiagnostic(severity, id);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "build.ps1")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found from test directory.");
    }

    private sealed record AnalyzerDiagnostic(string Severity, string Id);

    private sealed record PackageReference(
        string Include,
        string Version,
        string? PrivateAssets,
        string? IncludeAssets);

    private sealed record BuildResult(int ExitCode, string Output);
}
