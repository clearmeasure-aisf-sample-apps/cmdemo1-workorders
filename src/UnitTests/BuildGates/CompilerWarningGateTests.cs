using System.Diagnostics;
using System.Xml.Linq;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CompilerWarningGateTests
{
    [Test]
    public void ShouldConfigurePrivateBuildCompile_WhenBuildScriptIsRead()
    {
        string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "build.ps1"));
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
        var props = XDocument.Load(Path.Combine(FindRepoRoot(), "src", "Directory.Build.props"));

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

        string[] auditSuppressions = props.Descendants()
            .Where(element => element.Name.LocalName == "NuGetAuditSuppress")
            .Select(element => element.Attribute("Include")?.Value ?? "")
            .Order(StringComparer.Ordinal)
            .ToArray();
        auditSuppressions.ShouldBe(new[]
        {
            "https://github.com/advisories/GHSA-2fmv-rv2c-59x7",
            "https://github.com/advisories/GHSA-7hqp-7c5m-4vq6",
            "https://github.com/advisories/GHSA-846f-f764-76mx",
            "https://github.com/advisories/GHSA-c399-9853-6jmj",
            "https://github.com/advisories/GHSA-g27p-qf6j-c96c",
            "https://github.com/advisories/GHSA-qhrr-8q5h-9q3h",
            "https://github.com/advisories/GHSA-qjpx-jqm5-h2c4",
            "https://github.com/advisories/GHSA-r955-8q5j-3h83"
        });
    }

    [Test]
    public void ShouldNotDisableWarningsAsErrors_WhenProjectFilesAreRead()
    {
        string repoRoot = FindRepoRoot();
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
        string repoRoot = FindRepoRoot();
        string privateBuild = File.ReadAllText(Path.Combine(repoRoot, "PrivateBuild.ps1"));
        privateBuild.ShouldContain("dotnet format");
        privateBuild.ShouldContain("--verify-no-changes");

        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github", "workflows", "build.yml"));
        workflow.ShouldContain("dotnet format src/ChurchBulletin.sln --verify-no-changes");
        workflow.ShouldContain("/p:TreatWarningsAsErrors=true");
        workflow.ShouldContain("/p:EnforceCodeStyleInBuild=true");
    }

    [Test]
    public void ShouldDefineHouseStyle_WhenEditorConfigIsRead()
    {
        string config = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", ".editorconfig"));

        config.ShouldContain("csharp_style_var_for_built_in_types = false:warning");
        config.ShouldContain("csharp_style_var_when_type_is_apparent = true:suggestion");
        config.ShouldContain("csharp_style_var_elsewhere = true:suggestion");
        config.ShouldContain("csharp_prefer_braces = true:warning");
        config.ShouldContain("csharp_style_namespace_declarations = file_scoped:warning");
        config.ShouldContain("csharp_style_expression_bodied_methods = false:silent");
        config.ShouldContain("csharp_using_directive_placement = outside_namespace:warning");
        config.ShouldContain("dotnet_sort_system_directives_first = true:warning");
        config.ShouldContain("dotnet_separate_import_directive_groups = true:warning");
        config.ShouldContain("dotnet_naming_rule.interface_should_be_begins_with_i.severity = suggestion");
        config.ShouldContain("dotnet_naming_rule.private_instance_fields_should_be_underscore_camel.severity = suggestion");
        config.ShouldContain("end_of_line = lf");
    }

    [Test]
    public async Task ShouldFailOnlyStrictBuild_WhenCompilerWarningIsPresent()
    {
        string projectDirectory = Path.Combine(
            Path.GetTempPath(),
            $"compiler-warning-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "WarningGate.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(
                Path.Combine(projectDirectory, "Program.cs"),
                """
                #warning Warning gate contract
                System.Console.WriteLine("warning gate");
                """);

            var ordinaryBuild = await RunDotNetBuild(projectDirectory);
            var strictBuild = await RunDotNetBuild(
                projectDirectory,
                "/p:TreatWarningsAsErrors=true",
                "/p:MSBuildTreatAllWarningsAsErrors=true");

            ordinaryBuild.ExitCode.ShouldBe(0, ordinaryBuild.Output);
            ordinaryBuild.Output.ShouldContain("warning CS1030");
            strictBuild.ExitCode.ShouldNotBe(0, strictBuild.Output);
            strictBuild.Output.ShouldContain("error CS1030");
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    private static async Task<BuildResult> RunDotNetBuild(
        string workingDirectory,
        params string[] additionalArguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--no-incremental");
        foreach (string argument in additionalArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet build.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new BuildResult(
            process.ExitCode,
            $"{await standardOutput}{Environment.NewLine}{await standardError}");
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

    private sealed record BuildResult(int ExitCode, string Output);
}
