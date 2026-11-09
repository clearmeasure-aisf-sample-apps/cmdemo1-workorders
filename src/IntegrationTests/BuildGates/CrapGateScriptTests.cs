using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates;

[TestFixture]
public class CrapGateScriptTests
{
    [TestCase("crap-production-violations-pass.json", 0)]
    [TestCase("crap-production-violations-fail.json", 1)]
    [TestCase("crap-production-violations-negative.json", 2)]
    [TestCase("crap-production-violations-malformed.json", 2)]
    [TestCase("crap-production-violations-inconsistent.json", 2)]
    public async Task AssertCrapGate_WhenGivenAViolationsReport_ExitsWithItsVerdict(string fixtureFileName, int exitCode)
    {
        var result = await AssertCrapGateAsync(Fixture(fixtureFileName));

        result.ExitCode.ShouldBe(exitCode, result.Output);
    }

    [Test]
    public async Task FlattenCobertura_WhenAsyncStateMachine_CopiesHitsOntoOriginalMethod()
    {
        using var output = new ScratchTree("crap-flat");

        var xml = await FlattenAsync(Fixture("cobertura-async-state-machine.xml"), output);

        xml.ShouldContain("name=\"ExecuteCommandAsync\"");
        xml.ShouldContain("number=\"90\"");
    }

    [Test]
    public async Task FlattenCobertura_WhenOnlyStateMachineClassExists_SynthesizesParentMethod()
    {
        using var output = new ScratchTree("crap-orphan");

        var xml = await FlattenAsync(Fixture("cobertura-orphan-async-state-machine.xml"), output);

        xml.ShouldContain("name=\"ClearMeasure.Bootcamp.UI.Server.RequestBodyBufferingPipeline\"");
        xml.ShouldContain("name=\"InvokeAsync\"");
    }

    private static string Fixture(string fileName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "BuildGates", "Fixtures", fileName);
        File.Exists(path).ShouldBeTrue(path);
        return path;
    }

    private static Task<CommandOutcome> AssertCrapGateAsync(string violationsReport)
    {
        var script = RepositoryCheckout.Current.PathOf("scripts/crap/assert-crap-gate.ps1");
        return ExternalCommand.Pwsh(script, "-ViolationsPath", violationsReport).RunAsync();
    }

    private static async Task<string> FlattenAsync(string cobertura, ScratchTree output)
    {
        var script = RepositoryCheckout.Current.PathOf("scripts/crap/flatten-cobertura.csx");
        var flattened = output.PathOf("flattened.cobertura.xml");
        var command = await DotnetScriptTool.RunningAsync(script, flattened, cobertura);

        var result = await command.RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        return await File.ReadAllTextAsync(flattened);
    }
}
