using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class ExternalCommandTests
{
    private const string ReportScript = """
        param([string] $Name)
        Write-Output "hello $Name from $((Get-Location).Path)"
        [Console]::Error.WriteLine('to stderr')
        exit 3
        """;

    [Test]
    public async Task RunAsync_WhenScriptExits_ReturnsItsExitCodeAndBothStreams()
    {
        using var tree = new ScratchTree("external-command").Write("report.ps1", ReportScript);

        var result = await ExternalCommand.Pwsh(tree.PathOf("report.ps1"), "-Name", "gate").In(tree.Root).RunAsync();

        result.ExitCode.ShouldBe(3, result.Output);
        result.Output.ShouldContain($"hello gate from {Path.GetFullPath(tree.Root)}");
        result.Output.ShouldContain("to stderr");
    }

    [Test]
    public void In_WhenGivenADirectory_KeepsTheCommandAndChangesOnlyWhereItStarts()
    {
        var command = ExternalCommand.Dotnet("build", "--nologo");

        var moved = command.In("/work");

        moved.ShouldBe(command with { WorkingDirectory = "/work" });
        moved.Arguments.ShouldBe(["build", "--nologo"]);
    }

    [Test]
    public void Pwsh_WhenGivenAScript_RunsItWithoutAProfile()
    {
        var command = ExternalCommand.Pwsh("gate.ps1", "-SelfTest");

        command.FileName.ShouldBe("pwsh");
        command.Arguments.ShouldBe(["-NoProfile", "-File", "gate.ps1", "-SelfTest"]);
    }
}
