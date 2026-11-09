using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

[TestFixture]
public class DotnetScriptToolTests
{
    private const string EchoScript = """
        System.Console.WriteLine("args: " + string.Join("|", Args));
        """;

    [Test]
    public async Task RunningAsync_WhenGivenAScript_PassesTheArgumentsAfterTheScript()
    {
        using var tree = new ScratchTree("dotnet-script-tool").Write("echo.csx", EchoScript);
        var command = await DotnetScriptTool.RunningAsync(tree.PathOf("echo.csx"), "first", "second value");

        var result = await command.RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("args: first|second value");
    }
}
