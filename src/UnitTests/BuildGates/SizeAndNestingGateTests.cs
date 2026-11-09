using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class SizeAndNestingGateTests
{
    private const string ProductionPath = "src/Core/Sample.cs";
    private const string TestPath = "src/UnitTests/SampleTests.cs";

    [Test]
    public void ShouldUseTeachingLimits_WhenGateIsConfigured()
    {
        SizeAndNestingLimits.ProductionFileLines.ShouldBe(300);
        SizeAndNestingLimits.TestFileLines.ShouldBe(600);
        SizeAndNestingLimits.MethodLines.ShouldBe(30);
        SizeAndNestingLimits.MaxParameters.ShouldBe(4);
        SizeAndNestingLimits.MaxNesting.ShouldBe(3);
    }

    [Test]
    public void ShouldFlagFile_WhenProductionSourceExceeds300Lines()
    {
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, Lines(301));

        Keys(hits, SizeAndNestingLimits.FileRule).ShouldBe([$"file {ProductionPath}"]);
    }

    [Test]
    public void ShouldPassFile_WhenProductionSourceIs300Lines()
    {
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, Lines(300));

        Keys(hits, SizeAndNestingLimits.FileRule).ShouldBeEmpty();
    }

    [Test]
    public void ShouldPassFile_WhenTestSourceStaysWithin600Lines()
    {
        var hits = SizeAndNestingScanner.ScanFile(TestPath, Lines(600));

        Keys(hits, SizeAndNestingLimits.FileRule).ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagFile_WhenTestSourceExceeds600Lines()
    {
        var hits = SizeAndNestingScanner.ScanFile(TestPath, Lines(601));

        Keys(hits, SizeAndNestingLimits.FileRule).ShouldBe([$"file {TestPath}"]);
    }

    [Test]
    public void ShouldFlagMethod_WhenSpanExceedsThirtyLines()
    {
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, MethodSample(31));

        Keys(hits, SizeAndNestingLimits.MethodRule).ShouldContain("method src/Core/Sample.cs Sample.Run()");
    }

    [Test]
    public void ShouldPassMethod_WhenSpanIsThirtyLines()
    {
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, MethodSample(30));

        Keys(hits, SizeAndNestingLimits.MethodRule).ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagParameters_WhenSignatureHasFiveParameters()
    {
        var source = "class Sample { void Run(int a, int b, int c, int d, int e) { } }\n";
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, source);

        Keys(hits, SizeAndNestingLimits.ParametersRule)
            .ShouldBe(["parameters src/Core/Sample.cs Sample.Run(int,int,int,int,int)"]);
    }

    [Test]
    public void ShouldIgnoreSpacingInsideParameterTypes_WhenSignaturesMatch()
    {
        var spaced = "class Sample { void Run(int a, int b, int c, Func<int, string> work, int e) { } }\n";
        var tight = "class Sample { void Run(int a, int b, int c, Func<int,string> work, int e) { } }\n";
        var spacedHits = SizeAndNestingScanner.ScanFile(ProductionPath, spaced);
        var tightHits = SizeAndNestingScanner.ScanFile(ProductionPath, tight);
        var spacedKeys = Keys(spacedHits, SizeAndNestingLimits.ParametersRule);
        var tightKeys = Keys(tightHits, SizeAndNestingLimits.ParametersRule);

        spacedKeys.ShouldBe(tightKeys);
        tightKeys.ShouldBe(["parameters src/Core/Sample.cs Sample.Run(int,int,int,Func<int,string>,int)"]);
    }

    [Test]
    public void ShouldPassParameters_WhenSignatureHasFourParameters()
    {
        var source = "class Sample { void Run(this string name, int a, int b, int c) { } }\n";
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, source);

        Keys(hits, SizeAndNestingLimits.ParametersRule).ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagPrimaryConstructor_WhenItHasFiveParameters()
    {
        var source = "record Sample(int A, int B, int C, int D, int E);\n";
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, source);

        Keys(hits, SizeAndNestingLimits.ParametersRule)
            .ShouldBe(["parameters src/Core/Sample.cs Sample(int,int,int,int,int)"]);
    }

    [Test]
    public void ShouldFlagNesting_WhenControlFlowExceedsThree()
    {
        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, NestedIfs(4));

        Keys(hits, SizeAndNestingLimits.NestingRule).ShouldBe(["nesting src/Core/Sample.cs Sample.Run(int)"]);
    }

    [Test]
    public void ShouldPassNesting_WhenElseIfStaysAtTheSameDepth()
    {
        var source = """
            class Sample
            {
                void Run(int value)
                {
                    if (value == 1) { var a = 1; }
                    else if (value == 2) { var b = 2; }
                    else if (value == 3) { var c = 3; }
                    else if (value == 4) { var d = 4; }
                }
            }
            """;

        var hits = SizeAndNestingScanner.ScanFile(ProductionPath, source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPass_WhenSampleMeetsEveryLimit()
    {
        var source = """
            class Sample
            {
                void Run(int a, int b, int c, int d)
                {
                    if (a == 1)
                    {
                        if (b == 1)
                        {
                            if (c == 1)
                            {
                                var value = d;
                            }
                        }
                    }
                }
            }
            """;

        SizeAndNestingScanner.ScanFile(ProductionPath, source).ShouldBeEmpty();
    }

    [Test]
    public void ShouldIgnoreAutoGeneratedFiles_WhenHeaderIsPresent()
    {
        var text = "// <auto-generated>\n" + Lines(400);

        SizeAndNestingScanner.ScanFile(ProductionPath, text).ShouldBeEmpty();
    }

    [Test]
    public void ShouldSkipBuildOutputAndGeneratedFiles_WhenRepositoryIsScanned()
    {
        using var tree = TreeWithOversizedFilesInEveryExcludedPlace();

        var keys = SizeAndNestingScanner.ScanRepository(tree.Checkout);

        keys.ShouldBe(["file src/Core/Wide.cs"]);
    }

    private static IReadOnlyList<string> Keys(IReadOnlyList<SizeViolation> hits, string rule)
    {
        return hits.Where(hit => hit.Rule == rule).Select(hit => hit.Key).ToArray();
    }

    private static string Lines(int count)
    {
        return string.Join('\n', Enumerable.Repeat("// kept", count));
    }

    private static string MethodSample(int methodLines)
    {
        var lines = new List<string> { "class Sample", "{", "    void Run()", "    {" };
        for (var i = 0; i < methodLines - 3; i++)
        {
            lines.Add($"        var value{i} = {i};");
        }

        lines.Add("    }");
        lines.Add("}");
        return string.Join('\n', lines);
    }

    private static string NestedIfs(int depth)
    {
        var open = string.Concat(Enumerable.Repeat("if (value == 1) { ", depth));
        var close = string.Concat(Enumerable.Repeat("}", depth));
        return "class Sample { void Run(int value) { " + open + "var x = 1; " + close + " } }\n";
    }

    private static ScratchTree TreeWithOversizedFilesInEveryExcludedPlace()
    {
        return new ScratchTree("size-gate")
            .Write("src/Core/Wide.cs", Lines(301))
            .Write("src/Core/obj/Wide.cs", Lines(301))
            .Write("src/Core/Foo.g.cs", Lines(400))
            .Write("src/UI/Server/Generated/Wide.cs", Lines(400))
            .Write("src/UnitTests/TestResults/Wide.cs", Lines(700))
            .Write("src/Core/Narrow.cs", "class Narrow { void Run() { } }\n");
    }
}
