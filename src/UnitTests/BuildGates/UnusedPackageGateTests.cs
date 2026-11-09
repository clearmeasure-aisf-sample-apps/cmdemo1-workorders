using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class UnusedPackageGateTests
{
    [Test]
    public void SelectUnused_WhenCompileAssemblyIsUnreferenced_FlagsThePackage()
    {
        var unused = Select("src/Core/Core.csproj", "FluentValidation", ["FluentValidation"], ["mscorlib"]);

        unused.Count.ShouldBe(1);
        unused[0].PackageId.ShouldBe("FluentValidation");
    }

    [Test]
    public void SelectUnused_WhenCompileAssemblyIsReferenced_Passes()
    {
        var unused = Select("src/Core/Core.csproj", "FluentValidation", ["FluentValidation"], ["FluentValidation"]);

        unused.ShouldBeEmpty();
    }

    [Test]
    public void SelectUnused_WhenPackageHasNoAssemblies_TreatsItAsTooling()
    {
        var assemblies = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["coverlet.msbuild"] = new HashSet<string>(),
        };

        var unused = UnusedPackageGate.SelectUnused(
            "src/UnitTests/UnitTests.csproj",
            ["coverlet.msbuild"],
            assemblies,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        unused.ShouldBeEmpty();
    }

    [Test]
    public void NotInBaseline_WhenPackageIsNew_Fails()
    {
        var found = new[] { new UnusedPackage("src/Core/Core.csproj", "LeftOver") };
        var baseline = new[]
        {
            new PackageBaselineEntry("src/Core/Core.csproj", "Kept", "security pin"),
        };

        var unexpected = UnusedPackageGate.NotInBaseline(found, baseline);

        unexpected.Count.ShouldBe(1);
        unexpected[0].PackageId.ShouldBe("LeftOver");
    }

    [Test]
    public void NotInBaseline_WhenFoundSetShrinks_Passes()
    {
        var found = Array.Empty<UnusedPackage>();
        var baseline = new[]
        {
            new PackageBaselineEntry("src/Core/Core.csproj", "Kept", "security pin"),
        };

        baseline[0].Reason.ShouldBe("security pin");
        UnusedPackageGate.NotInBaseline(found, baseline).ShouldBeEmpty();
    }

    [Test]
    public void ReadBaseline_WhenReasonIsBlank_Fails()
    {
        using var tree = new ScratchTree("unused-baseline").Write(UnusedPackageGate.BaselinePath, """
            { "entries": [ { "project": "src/Core/Core.csproj", "package": "X", "reason": "  " } ] }
            """);

        var error = Should.Throw<InvalidOperationException>(() => UnusedPackageGate.ReadBaseline(tree.Checkout));

        error.Message.ShouldContain("no reason");
    }

    [Test]
    public void ReadAssemblyReferences_WhenGivenThisAssembly_SeesNUnit()
    {
        var names = UnusedPackageGate.ReadAssemblyReferences(typeof(UnusedPackageGateTests).Assembly.Location);

        names.ShouldContain("nunit.framework");
    }

    [Test]
    public void Solution_UnusedPackages_StayWithinBaseline()
    {
        var checkout = RepositoryCheckout.Current;
        var scan = UnusedPackageGate.ScanSolution(checkout, UnusedPackageGate.Configuration());
        if (scan.MissingOutputs.Count > 0 && !UnusedPackageGate.RequireOutputs)
        {
            Assert.Ignore("Project outputs are not built yet: " + string.Join(", ", scan.MissingOutputs));
        }

        scan.MissingOutputs.ShouldBeEmpty();
        var baseline = UnusedPackageGate.ReadBaseline(checkout);
        var unexpected = UnusedPackageGate.NotInBaseline(scan.Unused, baseline);
        unexpected.ShouldBeEmpty(Format(scan.Unused, unexpected));
    }

    private static IReadOnlyList<UnusedPackage> Select(
        string project,
        string packageId,
        IEnumerable<string> assemblyNames,
        IEnumerable<string> referenced)
    {
        var assemblies = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [packageId] = new HashSet<string>(assemblyNames, StringComparer.OrdinalIgnoreCase),
        };
        return UnusedPackageGate.SelectUnused(
            project,
            [packageId],
            assemblies,
            new HashSet<string>(referenced, StringComparer.OrdinalIgnoreCase));
    }

    private static string Format(IReadOnlyList<UnusedPackage> found, IReadOnlyList<UnusedPackage> unexpected)
    {
        var foundText = string.Join("; ", found.Select(item => item.Project + ":" + item.PackageId));
        var extra = string.Join("; ", unexpected.Select(item => item.Project + ":" + item.PackageId));
        return "unexpected [" + extra + "] found [" + foundText + "]";
    }
}
