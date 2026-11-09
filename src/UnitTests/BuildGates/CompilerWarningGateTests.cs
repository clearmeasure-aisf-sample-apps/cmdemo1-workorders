using System.Xml.Linq;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CompilerWarningGateTests
{
    private static readonly string[] StrictBuildProperties =
        ["/p:TreatWarningsAsErrors=true", "/p:MSBuildTreatAllWarningsAsErrors=true"];

    [Test]
    public void ShouldConfigurePrivateBuildCompile_WhenBuildScriptIsRead()
    {
        string source = RepositoryCheckout.Current.ReadText("build.ps1");
        int compileStart = source.IndexOf("Function Compile", StringComparison.Ordinal);
        int compileEnd = source.IndexOf("Function UnitTests", compileStart, StringComparison.Ordinal);

        compileStart.ShouldBeGreaterThan(-1);
        compileEnd.ShouldBeGreaterThan(compileStart);
        string compile = source[compileStart..compileEnd];
        compile.ShouldContain("/p:TreatWarningsAsErrors=\"true\"");
        compile.ShouldContain("/p:MSBuildTreatAllWarningsAsErrors=\"true\"");
    }

    [Test]
    public void ShouldTreatWarningsAsErrors_WhenDirectoryBuildPropsIsRead()
    {
        var props = XDocument.Parse(RepositoryCheckout.Current.ReadText("src/Directory.Build.props"));

        var treatWarningsAsErrors = props.Descendants()
            .Single(element => element.Name.LocalName == "TreatWarningsAsErrors");
        bool.Parse(treatWarningsAsErrors.Value).ShouldBeTrue();
        treatWarningsAsErrors.Attribute("Condition").ShouldBeNull();

        var enforceCodeStyle = props.Descendants()
            .Single(element => element.Name.LocalName == "EnforceCodeStyleInBuild");
        bool.Parse(enforceCodeStyle.Value).ShouldBeTrue();

        var analysisLevel = props.Descendants()
            .Single(element => element.Name.LocalName == "AnalysisLevel");
        analysisLevel.Value.ShouldBe("10.0");

        var warningsNotAsErrors = props.Descendants()
            .Single(element => element.Name.LocalName == "WarningsNotAsErrors");
        warningsNotAsErrors.Value.ShouldBe("CS0168;CS0219;CS1998;IDE0051;IDE0052");
        string condition = warningsNotAsErrors.Attribute("Condition")?.Value ?? "";
        condition.ShouldContain("Debug");
        condition.ShouldNotContain("Release");
    }

    [Test]
    public void ShouldSuppressTodaysNuGetAdvisories_WhenDirectoryBuildPropsIsRead()
    {
        var props = XDocument.Parse(RepositoryCheckout.Current.ReadText("src/Directory.Build.props"));
        string[] auditSuppressions = props.Descendants()
            .Where(element => element.Name.LocalName == "NuGetAuditSuppress")
            .Select(element => element.Attribute("Include")?.Value ?? "")
            .Order(StringComparer.Ordinal)
            .ToArray();
        auditSuppressions.ShouldBe(
        [
            "https://github.com/advisories/GHSA-2fmv-rv2c-59x7",
            "https://github.com/advisories/GHSA-7hqp-7c5m-4vq6",
            "https://github.com/advisories/GHSA-846f-f764-76mx",
            "https://github.com/advisories/GHSA-c399-9853-6jmj",
            "https://github.com/advisories/GHSA-g27p-qf6j-c96c",
            "https://github.com/advisories/GHSA-qhrr-8q5h-9q3h",
            "https://github.com/advisories/GHSA-qjpx-jqm5-h2c4",
            "https://github.com/advisories/GHSA-r955-8q5j-3h83"
        ]);
    }

    [Test]
    public void ShouldNotDisableWarningsAsErrors_WhenProjectFilesAreRead()
    {
        string repoRoot = RepositoryCheckout.Current.Root;
        var projectFiles = Directory
            .EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(repoRoot, "Directory.Build.props", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(repoRoot, "Directory.Build.targets", SearchOption.AllDirectories));

        string[] disabled = projectFiles
            .SelectMany(path => XDocument.Load(path)
                .Descendants()
                .Where(element =>
                    element.Name.LocalName is
                        "TreatWarningsAsErrors" or "MSBuildTreatAllWarningsAsErrors"
                    && bool.TryParse(element.Value, out bool enabled)
                    && !enabled)
                .Select(_ => path))
            .ToArray();

        disabled.ShouldBeEmpty();
    }

    [Test]
    public void ShouldVerifyFormatting_WhenPrivateBuildAndCiAreRead()
    {
        string privateBuild = RepositoryCheckout.Current.ReadText("PrivateBuild.ps1");
        privateBuild.ShouldContain("dotnet format");
        privateBuild.ShouldContain("--verify-no-changes");

        string workflow = RepositoryCheckout.Current.ReadText(".github/workflows/build.yml");
        workflow.ShouldContain("dotnet format style src/ChurchBulletin.sln --verify-no-changes --severity warn");
        workflow.ShouldContain("/p:TreatWarningsAsErrors=true");
        workflow.ShouldContain("/p:EnforceCodeStyleInBuild=true");
    }

    [Test]
    public void ShouldDefineHouseStyle_WhenEditorConfigIsRead()
    {
        string config = RepositoryCheckout.Current.ReadText("src/.editorconfig");

        config.ShouldContain("csharp_style_var_for_built_in_types = false:silent");
        config.ShouldContain("csharp_style_var_when_type_is_apparent = true:silent");
        config.ShouldContain("csharp_style_var_elsewhere = true:silent");
        config.ShouldContain("csharp_style_implicit_object_creation_when_type_is_apparent = true:silent");
        config.ShouldContain("csharp_prefer_braces = true:silent");
        config.ShouldContain("csharp_style_namespace_declarations = file_scoped:warning");
        config.ShouldContain("csharp_style_expression_bodied_methods = false:silent");
        config.ShouldContain("csharp_using_directive_placement = outside_namespace:silent");
        config.ShouldContain("dotnet_naming_rule.interface_should_be_begins_with_i.severity = suggestion");
        config.ShouldContain("dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion");
        config.ShouldContain("dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = suggestion");
        config.ShouldNotContain("private_instance_fields_should_be_underscore_camel");
        config.ShouldContain("end_of_line = lf");
    }

    [Test]
    public async Task ShouldFailOnlyStrictBuild_WhenCompilerWarningIsPresent()
    {
        using var project = CompilerWarningProjectMother.ProgramWithAWarningDirective();

        var ordinaryBuild = await BuildAsync(project);
        var strictBuild = await BuildAsync(project, StrictBuildProperties);

        ShouldSucceedWithTheWarning(ordinaryBuild);
        ShouldFailWithTheWarningAsAnError(strictBuild);
    }

    private static Task<CommandOutcome> BuildAsync(ScratchTree project, params string[] properties)
    {
        return ExternalCommand.Dotnet(["build", "--nologo", "--no-incremental", .. properties]).In(project.Root).RunAsync();
    }

    private static void ShouldSucceedWithTheWarning(CommandOutcome build)
    {
        build.ExitCode.ShouldBe(0, build.Output);
        build.Output.ShouldContain($"warning {CompilerWarningProjectMother.WarningId}");
    }

    private static void ShouldFailWithTheWarningAsAnError(CommandOutcome build)
    {
        build.ExitCode.ShouldNotBe(0, build.Output);
        build.Output.ShouldContain($"error {CompilerWarningProjectMother.WarningId}");
    }
}
